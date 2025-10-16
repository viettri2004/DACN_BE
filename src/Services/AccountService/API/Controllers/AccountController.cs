using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.DTOs;
using AccountService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using src.Shared.Resources;
using src.Shared.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Shared.Application.Extension;
using AccountService.Infrastructure.Persistence.Repositories;

namespace src.Services.AccountService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAuthService _authservice;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;
        private readonly IAccountRepository _accountRepository;

        public AccountController(IAuthService accountService,
                                IStringLocalizer<SharedResources> localizer,
                                IEmailService emailService,
                                IOtpService otpService,
                                IAccountRepository accountRepository)
        {
            _otpService = otpService;
            _authservice = accountService;
            _emailService = emailService;
            _localizer = localizer;
            _accountRepository = accountRepository;
        }
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse>> Register([FromBody] RegisterDTO registerDTO)
        {
            var response = await _authservice.Register(registerDTO);

            return response.ToActionResult();
        }
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse>> Login([FromBody] LoginDTO loginDTO)
        {
            var (response, refreshToken) = await _authservice.LoginAsync(loginDTO);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(14)
                };
                Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
            }
            // Console.WriteLine(refreshToken);
            return response.ToActionResult();
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse>> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }

            var (response, newRefreshToken) = await _authservice.RefreshToken(refreshToken);

            if (!string.IsNullOrEmpty(newRefreshToken))
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(14)
                };
                Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);
            }
            else
            {
                Response.Cookies.Delete("refreshToken");
            }
            
            return response.ToActionResult();
        }

        [HttpPost("send-otp")]
        public async Task<ActionResult<ApiResponse>> SendOtp([FromBody] SendOtpDTO sendOtpDTO)
        {
            var response = await _emailService.SendEmailAsync(sendOtpDTO.email);

            return response.ToActionResult();
        }

        [HttpPost("verify-otp")]
        public async Task<ActionResult<ApiResponse>> VerifyOtp([FromBody] VerifiedOtpDTO dto)
        {
            var otpKey = $"ResetPassword:{dto.Email}";
            var isValidOtp = await _otpService.ValidateOtpAsync(otpKey, dto.Otp);
            if (!isValidOtp)
            {
                return BadRequest(new ApiResponse("Error", _localizer["InvalidOtp"].Value, null, false));
            }

            return Ok(new ApiResponse("Success", _localizer["OtpVerified"].Value, null, true));
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> GetMyProfile()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }

            var response = await _accountRepository.GetUserProfileAsync(userId);

            return response.ToActionResult();
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            var response = await _authservice.ResetPassword(dto.Email, dto.NewPassword);

            return response.ToActionResult();
        }

    }
}