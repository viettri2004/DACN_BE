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
using Microsoft.Extensions.Options;

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
        private readonly IUserService _userService;
        private readonly IGoogleAuthService _googleAuthService;

        public AccountController(IAuthService accountService,
                                IStringLocalizer<SharedResources> localizer,
                                IEmailService emailService,
                                IOtpService otpService,
                                IUserService userService,
                                IGoogleAuthService googleAuthService)
        {
            _otpService = otpService;
            _authservice = accountService;
            _emailService = emailService;
            _localizer = localizer;
            _userService = userService;
            _googleAuthService = googleAuthService;
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
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7)
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
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var (response, newRefreshToken) = await _authservice.RefreshToken(refreshToken);

            if (!string.IsNullOrEmpty(newRefreshToken))
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);
            }
            else
            {
                Response.Cookies.Delete("refreshToken", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });
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
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _userService.GetUserProfileAsync(userId);

            return response.ToActionResult();
        }

        [HttpPatch("update-profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> UpdateProfile([FromBody] UpdateUserProfileDTO dto)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _userService.UpdateUserProfileAsync(userId, dto);

            return response.ToActionResult();
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            var response = await _authservice.ResetPassword(dto.Email, dto.NewPassword);

            return response.ToActionResult();
        }

        [HttpGet("google-auth-url")]
        public async Task<ActionResult<ApiResponse>> GetGoogleAuthUrl()
        {
            string state = Guid.NewGuid().ToString("N");

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(15) 
            };
            Response.Cookies.Append("google_auth_state", state, cookieOptions);

            var url = await _googleAuthService.GetAuthorizationUrlAsync(state);

            return Ok(new ApiResponse("Success", _localizer["GoogleAuthUrlGenerated"].Value, url, true));
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string? state = null)
        {
            var savedState = Request.Cookies["google_auth_state"];

            Response.Cookies.Delete("google_auth_state");

            var (response, refreshToken, redirectUrl) = await _authservice.GoogleCallbackAsync(code, state, savedState);

            if (!string.IsNullOrEmpty(refreshToken))    
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
            }
            return Redirect(redirectUrl);
        }

        // [HttpPost("google-login")]
        // public async Task<ActionResult<ApiResponse>> GoogleLogin([FromBody] GoogleAuthDTO googleAuthDTO)
        // {
        //     var (response, refreshToken) = await _authservice.GoogleLoginAsync(googleAuthDTO);

        //     if (!string.IsNullOrEmpty(refreshToken))
        //     {
        //         var cookieOptions = new CookieOptions
        //         {
        //             HttpOnly = true,
        //             SameSite = SameSiteMode.None,
        //             Expires = DateTime.UtcNow.AddDays(14)
        //         };
        //         Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        //     }

        //     return response.ToActionResult();
        // }

        [HttpPost("request-instructor")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> RequestInstructor([FromBody] InstructorRequestDTO dto)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
             if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));

            var response = await _authservice.RequestInstructor(userId, dto);
            return response.ToActionResult();
        }

        [HttpGet("instructor-requests")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse>> GetInstructorRequests()
        {
            var response = await _authservice.GetInstructorRequests();
            return response.ToActionResult();
        }

        [HttpPost("approve-instructor-request")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse>> ApproveInstructorRequest([FromBody] ApproveRequestDTO dto)
        {
            var adminId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
             if (string.IsNullOrEmpty(adminId))
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));

            var response = await _authservice.ApproveInstructorRequest(dto, adminId);
            return response.ToActionResult();
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = await _authservice.LogoutAsync(refreshToken);

            Response.Cookies.Delete("refreshToken", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });

            return response.ToActionResult();
        }

        [HttpPost("global-logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> GlobalLogout()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _authservice.GlobalLogoutAsync(userId);

            Response.Cookies.Delete("refreshToken", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax });

            return response.ToActionResult();
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _authservice.ChangePasswordAsync(userId, dto);
            return response.ToActionResult();
        }

        [HttpGet("users")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse>> GetAllUsers()
        {
            var response = await _authservice.GetAllUsersAsync();
            return response.ToActionResult();
        }

        [HttpGet("instructors")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse>> GetAllInstructors()
        {
            var response = await _authservice.GetAllInstructorsAsync();
            return response.ToActionResult();
        }

        [HttpPost("ban-user")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse>> BanUser([FromBody] BanUserDTO dto)
        {
            var response = await _authservice.BanUserAsync(dto);
            return response.ToActionResult();
        }
    }
}