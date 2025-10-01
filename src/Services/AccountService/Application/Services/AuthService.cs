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

        public AuthService(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ITokenService tokenService,
            IStringLocalizer<SharedResources> localizer)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _localizer = localizer;
        }
        public async Task<ApiResponse> Register(RegisterDTO RegisterDTO)
        {
            var testMessage = _localizer["UsernameAlreadyExists"];
            Console.WriteLine($"Key: UsernameAlreadyExists");
            Console.WriteLine($"Value: {testMessage.Value}");
            Console.WriteLine($"ResourceNotFound: {testMessage.ResourceNotFound}");
            Console.WriteLine($"SearchedLocation: {testMessage.SearchedLocation}");
            
            if (await _userManager.FindByNameAsync(RegisterDTO.UserName) != null)
                return new ApiResponse("Error", _localizer["UsernameAlreadyExists"].Value, null, false);

            if (await _userManager.FindByEmailAsync(RegisterDTO.Email) != null)
                return new ApiResponse("Error", _localizer["EmailAlreadyExists"].Value, null, false);

            if (await _userManager.Users.AnyAsync(u => u.PhoneNumber == RegisterDTO.PhoneNumber))
                return new ApiResponse("Error", _localizer["PhoneNumberAlreadyExists"].Value, null, false);

            var user = new User
            {
                UserName = RegisterDTO.UserName,
                Email = RegisterDTO.Email,
                FullName = RegisterDTO.FullName,
                PhoneNumber = RegisterDTO.PhoneNumber,
                IsBanned = false
            };

            var result = await _userManager.CreateAsync(user, RegisterDTO.Password);
            if (!result.Succeeded)
                return new ApiResponse("BadRequest", string.Join("; ", result.Errors.Select(e => e.Description)), null, false);

            if (!await _roleManager.RoleExistsAsync(RegisterDTO.Role))
                await _roleManager.CreateAsync(new IdentityRole(RegisterDTO.Role));

            await _userManager.AddToRoleAsync(user, RegisterDTO.Role);

            return new ApiResponse("Success", _localizer["RegisterSuccess"].Value, new { user.Id, user.UserName, Role = RegisterDTO.Role }, true);
        }
    }
}