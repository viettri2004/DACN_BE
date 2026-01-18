using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.DTOs;

namespace AccountService.Application.Interfaces
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string idToken);
        Task<string> GetAuthorizationUrlAsync(string? state = null);
        Task<GoogleTokenResponse?> ExchangeCodeForTokenAsync(string code);
        Task<GoogleUserInfo?> GetGoogleUserAsync(string accessToken);
    }
}
