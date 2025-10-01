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

namespace src.Services.AccountService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAuthService _authservice;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public AccountController(IAuthService accountService, IStringLocalizer<SharedResources> localizer)
        {
            _authservice = accountService;
            _localizer = localizer;
        }
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse>> Register([FromBody] RegisterDTO registerDTO)
        {
            var response = await _authservice.Register(registerDTO);

            return response.Code switch
            {
                "Success" => Created("", response),
                "Error" => Conflict(response),
                "BadRequest" => BadRequest(response),
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

            return response.Code switch
            {
                "Success" => Ok(response),
                "Unauthorized" => Unauthorized(response),
                "NotFound" => NotFound(response),
                "BadRequest" => BadRequest(response),
                _ => StatusCode(500, response)
            };
        }
    }
}