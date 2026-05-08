using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using OrderingService.Domain.Entities;
using IdentityService.Domain.Entities;
using LearningService.Application.Services;
using LearningService.Application.Interfaces;
using LearningService.Domain.Entities;
using InteractionService.Application.DTOs;
using InteractionService.Application.Interfaces;
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using src.Shared.Resources;
using src.Shared.Domain.Entities;
using Microsoft.Extensions.Options;

namespace IdentityService.Application.Services
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
        private readonly INotificationService _notificationService;

        public AuthService(
            IAccountRepository accountRepository,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ITokenService tokenService,
            IStringLocalizer<SharedResources> localizer,
            IGoogleAuthService googleAuthService,
            IOptions<GoogleConfig> googleConfig,
            INotificationService notificationService)
        {
            _accountRepository = accountRepository;
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _localizer = localizer;
            _googleAuthService = googleAuthService;
            _googleConfig = googleConfig.Value;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse> Register(RegisterDTO RegisterDTO)
        {
            if (string.IsNullOrWhiteSpace(RegisterDTO.UserName))
                return new ApiResponse("BadRequest", _localizer["RequiredField", "Username"].Value, null, false);
            
            if (string.IsNullOrWhiteSpace(RegisterDTO.Email))
                return new ApiResponse("BadRequest", _localizer["RequiredField", "Email"].Value, null, false);

            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(RegisterDTO.Email))
                return new ApiResponse("BadRequest", _localizer["InvalidEmailFormat"].Value, null, false);

            if (string.IsNullOrWhiteSpace(RegisterDTO.Password))
                return new ApiResponse("BadRequest", _localizer["RequiredField", "Password"].Value, null, false);

            if (string.IsNullOrWhiteSpace(RegisterDTO.FullName))
                return new ApiResponse("BadRequest", _localizer["RequiredField", "FullName"].Value, null, false);

            if (await _userManager.FindByNameAsync(RegisterDTO.UserName) != null)
                return new ApiResponse("Conflict", _localizer["UsernameAlreadyExists"].Value, null, false);

            if (await _userManager.FindByEmailAsync(RegisterDTO.Email) != null)
                return new ApiResponse("Conflict", _localizer["EmailAlreadyExists"].Value, null, false);

            User user = new Student
            {
                UserName = RegisterDTO.UserName,
                Email = RegisterDTO.Email,
                FullName = RegisterDTO.FullName,
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
            if (string.IsNullOrWhiteSpace(loginDTO.Username))
                return (new ApiResponse("BadRequest", _localizer["RequiredField", "Username"].Value, null, false), "");

            if (string.IsNullOrWhiteSpace(loginDTO.Password))
                return (new ApiResponse("BadRequest", _localizer["RequiredField", "Password"].Value, null, false), "");

            var user = await _userManager.FindByNameAsync(loginDTO.Username);
            if (user == null)
                return (new ApiResponse("Unauthorized", _localizer["InvalidUsernamePassword"].Value, null, false), "");

            if (!await _userManager.CheckPasswordAsync(user, loginDTO.Password))
                return (new ApiResponse("Unauthorized", _localizer["InvalidUsernamePassword"].Value, null, false), "");

            if (user.IsBanned)
                return (new ApiResponse("Unauthorized", _localizer["AccountLocked"].Value, null, false), "");

            var roles = await _userManager.GetRolesAsync(user);

            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshTokenString = _tokenService.GenerateRefreshToken();
            
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7) // Refresh token valid for 7 days
            };
            await _accountRepository.StoreRefreshTokenAsync(refreshTokenEntity);

            var response = new ApiResponse("Success", _localizer["LoginSuccess"], new LoginResponseDTO
            {
                Email = user.Email ?? "",
                AvatarUrl = user.AvatarUrl ?? "",
                FullName = user.FullName,
                AccessToken = accessToken,
            }, true);

            return (response, refreshTokenString);
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
                var refreshTokenString = _tokenService.GenerateRefreshToken();
                
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshTokenString,
                    UserId = existingUser.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7)
                };
                await _accountRepository.StoreRefreshTokenAsync(refreshTokenEntity);

                var response = new ApiResponse("Success", _localizer["LoginSuccess"].Value, new LoginResponseDTO
                {
                    Email = existingUser.Email ?? "",
                    AvatarUrl = existingUser.AvatarUrl ?? "",
                    FullName = existingUser.FullName,
                    AccessToken = accessToken,
                }, true);

                return (response, refreshTokenString);
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
                var refreshTokenString = _tokenService.GenerateRefreshToken();
                
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshTokenString,
                    UserId = newUser.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7)
                };
                await _accountRepository.StoreRefreshTokenAsync(refreshTokenEntity);

                var response = new ApiResponse("Success", _localizer["LoginSuccess"].Value, new LoginResponseDTO
                {
                    Email = newUser.Email ?? "",
                    AvatarUrl = newUser.AvatarUrl ?? "",
                    FullName = newUser.FullName,
                    AccessToken = accessToken,
                }, true);

                return (response, refreshTokenString);
            }
        }

        public async Task<(ApiResponse response, string refreshToken)> RefreshToken(string refreshToken)
        {
            var storedToken = await _accountRepository.GetRefreshTokenAsync(refreshToken);
            if (storedToken == null || storedToken.IsRevoked)
                return (new ApiResponse("Unauthorized", _localizer["InvalidRefreshToken"].Value, null, false), "");

            var user = storedToken.User;

            if (storedToken.ExpiryDate < DateTime.UtcNow)
            {
                storedToken.IsRevoked = true;
                await _accountRepository.UpdateRefreshTokenAsync(storedToken);
                return (new ApiResponse("Unauthorized", _localizer["RefreshTokenExpired"].Value, null, false), "");
            }

            // Simple overwrite logic
            var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var newRefreshTokenString = _tokenService.GenerateRefreshToken();
            
            storedToken.Token = newRefreshTokenString;
            storedToken.ExpiryDate = DateTime.UtcNow.AddDays(7);
            storedToken.AddedDate = DateTime.UtcNow; // Optional: update added date to current

            await _accountRepository.UpdateRefreshTokenAsync(storedToken);

            return (new ApiResponse("Success", _localizer["RefreshTokenSuccess"].Value, new { AccessToken = newAccessToken, }, true), newRefreshTokenString);
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
                    var redirectUrl = _googleConfig.FrontendSuccessUrl;
                    return (response, refreshToken, redirectUrl);
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
                await _notificationService.CreateNotificationForRoleAsync(
                    NotificationRole.Admin,
                    _localizer["NewInstructorRequestTitle"].Value,
                    string.Format(_localizer["NewInstructorRequestMessage"].Value, user.FullName),
                    NotificationType.InstructorRequest,
                    user.Id
                );

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
            request.ProcessedAt = DateTime.UtcNow;
            
            NotificationType type = NotificationType.Other;

            if (dto.IsApproved)
            {
                request.Status = "Approved";
                
                var user = request.User;
                if (!await _roleManager.RoleExistsAsync("Instructor"))
                    await _roleManager.CreateAsync(new IdentityRole("Instructor"));
                
                await _userManager.AddToRoleAsync(user, "Instructor");
                await _accountRepository.UpdateUserDiscriminatorToInstructor(user.Id);
                
                type = NotificationType.InstructorRequestResult;
            }
            else
            {
                request.Status = "Rejected";
                type = NotificationType.InstructorRequestResult;
            }

            await _accountRepository.UpdateInstructorRequestAsync(request);

            var notification = new Notification
            {
                UserId = request.UserId,
                Title = dto.Title,
                Message = dto.Message,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                RelatedId = request.Id.ToString()
            };
            await _notificationService.CreateNotificationAsync(notification);
            
            return new ApiResponse("Success", dto.IsApproved ? _localizer["RequestApproved"].Value : _localizer["RequestRejected"].Value, null, true);
        }

        public async Task<ApiResponse> LogoutAsync(string? refreshToken)
        {
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _accountRepository.RevokeRefreshTokenAsync(refreshToken);
            }

            return new ApiResponse("Success", _localizer["LogoutSuccess"].Value, null, true);
        }

        public async Task<ApiResponse> GlobalLogoutAsync(string userId)
        {
            await _accountRepository.RevokeAllUserTokensAsync(userId);

            return new ApiResponse("Success", _localizer["GlobalLogoutSuccess"].Value, null, true);
        }

        public async Task<ApiResponse> ChangePasswordAsync(string userId, ChangePasswordDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);
            }

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault();
                string message = error?.Code == "PasswordMismatch" ? _localizer["InvalidOldPassword"].Value : (error?.Description ?? _localizer["UnexpectedError"].Value);
                return new ApiResponse("BadRequest", message, null, false);
            }

            return new ApiResponse("Success", _localizer["ChangePasswordSuccess"].Value, null, true);
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



