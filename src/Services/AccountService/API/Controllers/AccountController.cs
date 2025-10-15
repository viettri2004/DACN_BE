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

        public AccountController(IAuthService accountService, IStringLocalizer<SharedResources> localizer, IEmailService emailService, IOtpService otpService)
        {
            _otpService = otpService;
            _authservice = accountService;
            _emailService = emailService;
            _localizer = localizer;
        }
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse>> Register([FromBody] RegisterDTO registerDTO)
        {
            var response = await _authservice.Register(registerDTO);

            return response.Code switch
            {
                "Success" => Created("", response),
                "NotFound" => NotFound(response),
                "BadRequest" => BadRequest(response),
                "Unauthorized" => Unauthorized(response),
                "Conflict" => Conflict(response),
                _ => StatusCode(500, response)
            };
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
            return response.Code switch
            {
                "Success" => Ok(response),
                "NotFound" => NotFound(response),
                "BadRequest" => BadRequest(response),
                "Unauthorized" => Unauthorized(response),
                "Conflict" => Conflict(response),
                _ => StatusCode(500, response)
            };
        }
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse>> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = await _authservice.RefreshToken(refreshToken);

            return response.Code switch
            {
                "Success" => Created("", response),
                "NotFound" => NotFound(response),
                "BadRequest" => BadRequest(response),
                "Unauthorized" => Unauthorized(response),
                "Conflict" => Conflict(response),
                _ => StatusCode(500, response)
            };
        }
        [HttpPost("send-otp")]
        public async Task<ActionResult<ApiResponse>> SendOtp([FromQuery] string email)
        {
            var response = await _emailService.SendEmailAsync(email);

            return response.Code switch
            {
                "Success" => Created("", response),
                "NotFound" => NotFound(response),
                "BadRequest" => BadRequest(response),
                "Unauthorized" => Unauthorized(response),
                "Conflict" => Conflict(response),
                _ => StatusCode(500, response)
            };
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
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            var response = await _authservice.ResetPassword(dto.Email, dto.NewPassword);

            return response.Code switch
            {
                "Success" => Created("", response),
                "NotFound" => NotFound(response),
                "BadRequest" => BadRequest(response),
                "Unauthorized" => Unauthorized(response),
                "Conflict" => Conflict(response),
                _ => StatusCode(500, response)
            };
        }
        [Authorize(Policy = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult GetForAdmin()
        {
            return Ok("This is only for admins");
        }

        [Authorize(Policy = "Student")]
        [HttpGet("student-only")]
        public IActionResult GetForUser()
        {
            return Ok("This is only for students");
        }

        [Authorize(Policy = "Instructor")]
        [HttpGet("instructor-only")]
        public IActionResult GetForInstructor()
        {
            return Ok("This is only for instructors");
        }
    }
}