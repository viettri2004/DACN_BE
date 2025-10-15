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

namespace AccountService.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IAccountRepository _accountRepository;

        public AuthService(
            IAccountRepository accountRepository,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ITokenService tokenService,
            IStringLocalizer<SharedResources> localizer)
        {
            _accountRepository = accountRepository;
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _localizer = localizer;
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

            User user;
            if (RegisterDTO.Role == "Student")
                user = new Student
                {
                    UserName = RegisterDTO.UserName,
                    Email = RegisterDTO.Email,
                    FullName = RegisterDTO.FullName,
                    PhoneNumber = RegisterDTO.PhoneNumber,
                    IsBanned = false
                };
            else if (RegisterDTO.Role == "Instructor")
                user = new Instructor
                {
                    UserName = RegisterDTO.UserName,
                    Email = RegisterDTO.Email,
                    FullName = RegisterDTO.FullName,
                    PhoneNumber = RegisterDTO.PhoneNumber,
                    IsBanned = false
                };
            else
            {
                return new ApiResponse("BadRequest", _localizer["InvalidRole"].Value, null, false);
            }
            ;

            var result = await _userManager.CreateAsync(user, RegisterDTO.Password);
            if (!result.Succeeded)
                return new ApiResponse("BadRequest", string.Join("; ", result.Errors.Select(e => e.Description)), null, false);

            if (!await _roleManager.RoleExistsAsync(RegisterDTO.Role))
                await _roleManager.CreateAsync(new IdentityRole(RegisterDTO.Role));

            await _userManager.AddToRoleAsync(user, RegisterDTO.Role);

            return new ApiResponse("Success", _localizer["RegisterSuccess"].Value, new { user.Id, user.UserName, Role = RegisterDTO.Role }, true);
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

            var response = new ApiResponse("Success", _localizer["LoginSuccess"], new
            {
                AccessToken = accessToken,
            }, true);

            return (response, refreshToken);
        }
        public async Task<ApiResponse> RefreshToken(string refreshToken)
        {
            var user = await _accountRepository.GetUserFromRefreshToken(refreshToken);
            if (user == null)
                return new ApiResponse("Unauthorized", _localizer["InvalidRefreshToken"].Value, null, false);

            var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            await _tokenService.StoreRefreshTokenAsync(user, newRefreshToken);

            return new ApiResponse(
                "Success",
                _localizer["RefreshTokenSuccess"].Value,
                new
                {
                    AccessToken = newAccessToken,
                },
                true
            );
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

    }
}