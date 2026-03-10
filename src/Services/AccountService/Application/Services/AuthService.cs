using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.DTOs;
using AccountService.Application.Interfaces;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using src.Shared.Resources;
using src.Shared.Domain.Entities;
using Microsoft.Extensions.Options;
using Shared.Application.Interfaces;

namespace AccountService.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IAccountRepository _accountRepository;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly GoogleConfig _googleConfig;
        private readonly INotificationRepository _notificationRepository;

        public AuthService(
            IAccountRepository accountRepository,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ITokenService tokenService,
            IStringLocalizer<SharedResources> localizer,
            IGoogleAuthService googleAuthService,
            IOptions<GoogleConfig> googleConfig,
            INotificationRepository notificationRepository)
        {
            _accountRepository = accountRepository;
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _localizer = localizer;
            _googleAuthService = googleAuthService;
            _googleConfig = googleConfig.Value;
            _notificationRepository = notificationRepository;
        }

        public async Task<ApiResponse> Register(RegisterDTO RegisterDTO)
        {
            if (await _userManager.FindByNameAsync(RegisterDTO.UserName) != null)
                return new ApiResponse("Conflict", _localizer["UsernameAlreadyExists"].Value, null, false);

            if (await _userManager.FindByEmailAsync(RegisterDTO.Email) != null)
                return new ApiResponse("Conflict", _localizer["EmailAlreadyExists"].Value, null, false);

            if (await _userManager.Users.AnyAsync(u => u.PhoneNumber == RegisterDTO.PhoneNumber))
                return new ApiResponse("Conflict", _localizer["PhoneNumberAlreadyExists"].Value, null, false);

            User user = new Student
            {
                UserName = RegisterDTO.UserName,
                Email = RegisterDTO.Email,
                FullName = RegisterDTO.FullName,
                PhoneNumber = RegisterDTO.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                IsBanned = false
            };

            var result = await _userManager.CreateAsync(user, RegisterDTO.Password);
            if (!result.Succeeded)
                return new ApiResponse("BadRequest", string.Join("; ", result.Errors.Select(e => e.Description)), null, false);

            await _userManager.AddToRoleAsync(user, "Student");

            return new ApiResponse("Created", _localizer["RegisterSuccess"].Value, new { user.Id, user.UserName, Role = "Student" }, true);
        }

        public async Task<(ApiResponse response, string refreshToken)> LoginAsync(LoginDTO loginDTO)
        {
            var user = await _userManager.FindByNameAsync(loginDTO.Username);
            if (user == null)
                return (new ApiResponse("Unauthorized", _localizer["InvalidUsernamePassword"], null, false), "");

            if (!await _userManager.CheckPasswordAsync(user, loginDTO.Password))
                return (new ApiResponse("Unauthorized", _localizer["InvalidUsernamePassword"], null, false), "");

            if (user.IsBanned)
                return (new ApiResponse("Unauthorized", _localizer["AccountLocked"], null, false), "");

            var roles = await _userManager.GetRolesAsync(user);

            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            await _tokenService.StoreRefreshTokenAsync(user, refreshToken);

            var response = new ApiResponse("Success", _localizer["LoginSuccess"], new LoginResponseDTO
            {
                Email = user.Email ?? "",
                AvatarUrl = user.AvatarUrl ?? "",
                FullName = user.FullName,
                AccessToken = accessToken,
            }, true);

            return (response, refreshToken);
        }

        public async Task<(ApiResponse response, string refreshToken)> GoogleLoginAsync(string IdToken)
        {
            var googleUserInfo = await _googleAuthService.ValidateGoogleTokenAsync(IdToken);

            if (googleUserInfo == null)
            {
                return (new ApiResponse("Unauthorized", _localizer["InvalidGoogleToken"].Value, null, false), "");
            }

            var existingUser = await _userManager.FindByEmailAsync(googleUserInfo.Email);

            if (existingUser != null)
            {
                if (existingUser.IsBanned)
                {
                    return (new ApiResponse("Unauthorized", _localizer["AccountLocked"].Value, null, false), "");
                }

                var accessToken = await _tokenService.GenerateAccessTokenAsync(existingUser);
                var refreshToken = _tokenService.GenerateRefreshToken();
                await _tokenService.StoreRefreshTokenAsync(existingUser, refreshToken);

                var response = new ApiResponse("Success", _localizer["LoginSuccess"].Value, new LoginResponseDTO
                {
                    Email = existingUser.Email ?? "",
                    AvatarUrl = existingUser.AvatarUrl ?? "",
                    FullName = existingUser.FullName,
                    AccessToken = accessToken,
                }, true);

                return (response, refreshToken);
            }
            else
            {
                User newUser = new Student
                {
                    UserName = googleUserInfo.Email,
                    Email = googleUserInfo.Email,
                    FullName = googleUserInfo.Name,
                    AvatarUrl = googleUserInfo.Picture,
                    EmailConfirmed = googleUserInfo.EmailVerified,
                    IsBanned = false
                };

                var createResult = await _userManager.CreateAsync(newUser);
                if (!createResult.Succeeded)
                {
                    return (new ApiResponse("BadRequest", string.Join("; ", createResult.Errors.Select(e => e.Description)), null, false), "");
                }

                await _userManager.AddToRoleAsync(newUser, "Student");

                var accessToken = await _tokenService.GenerateAccessTokenAsync(newUser);
                var refreshToken = _tokenService.GenerateRefreshToken();
                await _tokenService.StoreRefreshTokenAsync(newUser, refreshToken);

                var response = new ApiResponse("Success", _localizer["LoginSuccess"].Value, new LoginResponseDTO
                {
                    Email = newUser.Email ?? "",
                    AvatarUrl = newUser.AvatarUrl ?? "",
                    FullName = newUser.FullName,
                    AccessToken = accessToken,
                }, true);

                return (response, refreshToken);
            }
        }

        public async Task<(ApiResponse response, string refreshToken)> RefreshToken(string refreshToken)
        {
            var user = await _accountRepository.GetUserFromRefreshToken(refreshToken);
            if (user == null)
                return (new ApiResponse("Unauthorized", _localizer["InvalidRefreshToken"].Value, null, false), "");

            var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            await _tokenService.StoreRefreshTokenAsync(user, newRefreshToken);

            return (new ApiResponse("Success", _localizer["RefreshTokenSuccess"].Value, new { AccessToken = newAccessToken, }, true), newRefreshToken);
        }

        public async Task<ApiResponse> ResetPassword(string email, string newPassword)
        {
            var user = await _accountRepository.FindUserByEmail(email);
            if (user == null)
            {
                return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);
            }

            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, newPassword);

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return new ApiResponse("Success", _localizer["PasswordResetSuccess"].Value, null, true);
            }

            return new ApiResponse("Error", _localizer["PasswordResetFailed"].Value, result.Errors, false);
        }

        public async Task<(ApiResponse response, string refreshToken, string redirectUrl)> GoogleCallbackAsync(string code, string? state, string? savedState)
        {
            try
            {
                if (string.IsNullOrEmpty(state) || state != savedState)
                {
                    return (new ApiResponse("Error", _localizer["InvalidStateCSRF"].Value, null, false), "", _googleConfig.FrontendFailUrl);
                }

                var tokenResponse = await _googleAuthService.ExchangeCodeForTokenAsync(code);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.IdToken))
                {
                    return (new ApiResponse("Error", _localizer["TokenExchangeFailed"].Value, null, false), "", _googleConfig.FrontendFailUrl);
                }

                var (response, refreshToken) = await GoogleLoginAsync(tokenResponse.IdToken);

                if (response.Success)
                {
                    var accessToken = (response.Data as LoginResponseDTO)?.AccessToken ?? "";
                    var redirectUrl = _googleConfig.FrontendSuccessUrl;
                    var separator = redirectUrl.Contains("?") ? "&" : "?";

                    return (response, refreshToken, $"{redirectUrl}{separator}accessToken={accessToken}");
                }

                return (response, "", _googleConfig.FrontendFailUrl);
            }
            catch (Exception)
            {
                return (new ApiResponse("Error", _localizer["UnexpectedError"].Value, null, false), "", _googleConfig.FrontendFailUrl);
            }
        }

        public async Task<ApiResponse> RequestInstructor(string userId, InstructorRequestDTO requestDTO)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                 return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);
            
            if (await _userManager.IsInRoleAsync(user, "Instructor"))
                 return new ApiResponse("Conflict", _localizer["UserAlreadyInstructor"].Value, null, false);

            var existingRequest = await _accountRepository.GetInstructorRequestByUserIdAsync(userId);
            if (existingRequest != null)
                return new ApiResponse("Conflict", _localizer["RequestAlreadySent"].Value, null, false);
            
            var request = new InstructorRequest
            {
                UserId = userId,
                Experience = requestDTO.Experience,
                Expertise = requestDTO.Expertise,
                Certificate = requestDTO.Certificate,
                Introduction = requestDTO.Introduction,
                SocialLinks = requestDTO.SocialLinks,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _accountRepository.CreateInstructorRequestAsync(request);
            if (result)
            {
                var notification = new Notification
                {
                    Title = "New Instructor Request",
                    Message = $"{user.FullName} has requested to become an instructor.",
                    Type = "InstructorRequest",
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                };
                await _notificationRepository.CreateNotificationAsync(notification);

                return new ApiResponse("Created", _localizer["RequestSubmittedSuccess"].Value, null, true);
            }
            
            return new ApiResponse("Error", _localizer["RequestSubmitFailed"].Value, null, false);
        }

        public async Task<ApiResponse> GetInstructorRequests()
        {
            var requests = await _accountRepository.GetPendingInstructorRequestsAsync();
            var dtos = requests.Select(r => new InstructorRequestViewDTO
            {
                Id = r.Id,
                UserId = r.UserId,
                FullName = r.User.FullName,
                Email = r.User.Email ?? "",
                Experience = r.Experience,
                Expertise = r.Expertise,
                Certificate = r.Certificate,
                Introduction = r.Introduction,
                SocialLinks = r.SocialLinks,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                ProcessedAt = r.ProcessedAt
            }).ToList();

            return new ApiResponse("Success", _localizer["PendingRequestsRetrieved"].Value, dtos, true);
        }

        public async Task<ApiResponse> ApproveInstructorRequest(ApproveRequestDTO dto, string adminId)
        {
            var request = await _accountRepository.GetInstructorRequestByIdAsync(dto.RequestId);
            if (request == null)
                return new ApiResponse("NotFound", _localizer["RequestNotFound"].Value, null, false);
            
            if (request.Status != "Pending")
                 return new ApiResponse("Conflict", _localizer["RequestNotPending"].Value, null, false);

            request.AdminId = adminId;
            request.AdminComment = dto.Reason;
            request.ProcessedAt = DateTime.UtcNow;
            
            string title = "";
            string message = "";

            if (dto.IsApproved)
            {
                request.Status = "Approved";
                
                var user = request.User;
                if (!await _roleManager.RoleExistsAsync("Instructor"))
                    await _roleManager.CreateAsync(new IdentityRole("Instructor"));
                
                await _userManager.AddToRoleAsync(user, "Instructor");
                await _accountRepository.UpdateUserDiscriminatorToInstructor(user.Id);
                
                title = "Instructor Request Approved";
                message = "Congratulations! Your request to become an instructor has been approved.";
            }
            else
            {
                request.Status = "Rejected";
                title = "Instructor Request Rejected";
                message = $"Sorry, your request to become an instructor has been rejected. Reason: {dto.Reason}";
            }

            await _accountRepository.UpdateInstructorRequestAsync(request);

            var notification = new Notification
            {
                UserId = request.UserId,
                Title = title,
                Message = message,
                Type = "InstructorRequestResult",
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepository.CreateNotificationAsync(notification);
            
            return new ApiResponse("Success", dto.IsApproved ? _localizer["RequestApproved"].Value : _localizer["RequestRejected"].Value, null, true);
        }

        public async Task<ApiResponse> LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);
            }

            await _tokenService.RemoveRefreshTokenAsync(user);

            return new ApiResponse("Success", _localizer["LogoutSuccess"].Value, null, true);
        }

        public async Task<ApiResponse> GetAllUsersAsync()
        {
            var users = await _accountRepository.GetAllUsersAsync();
            var userViewDtos = new List<UserViewDTO>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "Student";

                if (role == "Admin") continue;

                userViewDtos.Add(new UserViewDTO
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    FullName = user.FullName,
                    AvatarUrl = user.AvatarUrl,
                    Role = role,
                    IsBanned = user.IsBanned,
                    CreatedAt = user.CreatedAt
                });
            }

            return new ApiResponse("Success", _localizer["UsersRetrieved"].Value, userViewDtos, true);
        }

        public async Task<ApiResponse> GetAllInstructorsAsync()
        {
            var instructors = await _accountRepository.GetAllInstructorsAsync();
            var instructorDtos = instructors.Select(i => new UserViewDTO
            {
                Id = i.Id,
                UserName = i.UserName ?? "",
                Email = i.Email ?? "",
                FullName = i.FullName,
                AvatarUrl = i.AvatarUrl,
                Role = "Instructor",
                IsBanned = i.IsBanned,
                CreatedAt = i.CreatedAt
            }).ToList();

            return new ApiResponse("Success", _localizer["InstructorsRetrieved"].Value, instructorDtos, true);
        }

        public async Task<ApiResponse> BanUserAsync(BanUserDTO dto)
        {
            var user = await _accountRepository.GetUserByIdAsync(dto.UserId);
            if (user == null)
                return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);

            user.IsBanned = dto.IsBanned;
            await _accountRepository.UpdateUserAsync(user);

            var message = dto.IsBanned ? _localizer["UserBanned"].Value : _localizer["UserUnbanned"].Value;
            return new ApiResponse("Success", message, null, true);
        }
    }
}
