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

        public AuthService(
            IAccountRepository accountRepository,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ITokenService tokenService,
            IStringLocalizer<SharedResources> localizer,
            IGoogleAuthService googleAuthService,
            IOptions<GoogleConfig> googleConfig)
        {
            _accountRepository = accountRepository;
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _localizer = localizer;
            _googleAuthService = googleAuthService;
            _googleConfig = googleConfig.Value;
        }
        public async Task<ApiResponse> Register(RegisterDTO RegisterDTO)
        {
            // var testMessage = _localizer["UsernameAlreadyExists"];
            // Console.WriteLine($"Key: UsernameAlreadyExists");
            // Console.WriteLine($"Value: {testMessage.Value}");
            // Console.WriteLine($"ResourceNotFound: {testMessage.ResourceNotFound}");
            // Console.WriteLine($"SearchedLocation: {testMessage.SearchedLocation}");

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


            // if (RegisterDTO.Role == "Student")
            //     user = new Student
            //     {
            //         UserName = RegisterDTO.UserName,
            //         Email = RegisterDTO.Email,
            //         FullName = RegisterDTO.FullName,
            //         PhoneNumber = RegisterDTO.PhoneNumber,
            //         IsBanned = false
            //     };
            // else if (RegisterDTO.Role == "Instructor")
            //     user = new Instructor
            //     {
            //         UserName = RegisterDTO.UserName,
            //         Email = RegisterDTO.Email,
            //         FullName = RegisterDTO.FullName,
            //         PhoneNumber = RegisterDTO.PhoneNumber,
            //         IsBanned = false
            //     };
            // else if (RegisterDTO.Role == "Admin")
            //     user = new Admin
            //     {
            //         UserName = RegisterDTO.UserName,
            //         Email = RegisterDTO.Email,
            //         FullName = RegisterDTO.FullName,
            //         PhoneNumber = RegisterDTO.PhoneNumber,
            //         IsBanned = false
            //     };
            // else
            // {
            //     return new ApiResponse("BadRequest", _localizer["InvalidRole"].Value, null, false);
            // }
            // ;

            var result = await _userManager.CreateAsync(user, RegisterDTO.Password);
            if (!result.Succeeded)
                return new ApiResponse("BadRequest", string.Join("; ", result.Errors.Select(e => e.Description)), null, false);

            // if (!await _roleManager.RoleExistsAsync("Student"))
            //     await _roleManager.CreateAsync(new IdentityRole("Student"));

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
                // if (role == "Instructor")
                // {
                //     newUser = new Instructor
                //     {
                //         UserName = googleUserInfo.Email,
                //         Email = googleUserInfo.Email,
                //         FullName = googleUserInfo.Name,
                //         AvatarUrl = googleUserInfo.Picture,
                //         EmailConfirmed = googleUserInfo.EmailVerified,
                //         IsBanned = false
                //     };
                // }
                // else
                // {
                //     newUser = new Student
                //     {
                //         UserName = googleUserInfo.Email,
                //         Email = googleUserInfo.Email,
                //         FullName = googleUserInfo.Name,
                //         AvatarUrl = googleUserInfo.Picture,
                //         EmailConfirmed = googleUserInfo.EmailVerified,
                //         IsBanned = false
                //     };
                // }

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
                return new ApiResponse("Created", _localizer["RequestSubmittedSuccess"].Value, null, true);
            
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

        public async Task<ApiResponse> ApproveInstructorRequest(int requestId, string adminId, bool isApproved)
        {
            var request = await _accountRepository.GetInstructorRequestByIdAsync(requestId);
            if (request == null)
                return new ApiResponse("NotFound", _localizer["RequestNotFound"].Value, null, false);
            
            if (request.Status != "Pending")
                 return new ApiResponse("Conflict", _localizer["RequestNotPending"].Value, null, false);

            request.AdminId = adminId;
            request.ProcessedAt = DateTime.UtcNow;
            
            if (isApproved)
            {
                request.Status = "Approved";
                
                var user = request.User;
                if (!await _roleManager.RoleExistsAsync("Instructor"))
                    await _roleManager.CreateAsync(new IdentityRole("Instructor"));
                
                await _userManager.AddToRoleAsync(user, "Instructor");
                await _accountRepository.UpdateUserDiscriminatorToInstructor(user.Id);
            }
            else
            {
                request.Status = "Rejected";
            }

            await _accountRepository.UpdateInstructorRequestAsync(request);
            
            return new ApiResponse("Success", isApproved ? _localizer["RequestApproved"].Value : _localizer["RequestRejected"].Value, null, true);
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
    }
}