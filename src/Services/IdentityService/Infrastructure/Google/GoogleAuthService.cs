using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace IdentityService.Infrastructure.Google
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleConfig _googleConfig;

        public GoogleAuthService(HttpClient httpClient, IOptions<GoogleConfig> googleConfig)
        {
            _httpClient = httpClient;
            _googleConfig = googleConfig.Value;
        }

        public async Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string idToken)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var tokenInfo = await response.Content.ReadFromJsonAsync<GoogleTokenInfo>();

                if (tokenInfo == null)
                {
                    return null;
                }

                if (tokenInfo.Aud != _googleConfig.ClientId)
                {
                    return null;
                }

                if (tokenInfo.Exp < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    return null;
                }

                return new GoogleUserInfo
                {
                    Id = tokenInfo.Sub,
                    Email = tokenInfo.Email,
                    EmailVerified = tokenInfo.EmailVerified,
                    Name = tokenInfo.Name,
                    GivenName = tokenInfo.GivenName,
                    FamilyName = tokenInfo.FamilyName,
                    Picture = tokenInfo.Picture
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Task<string> GetAuthorizationUrlAsync(string? state = null)
        {
            var authEndpoint = string.IsNullOrEmpty(_googleConfig.AuthUri)
                ? "https://accounts.google.com/o/oauth2/v2/auth"
                : _googleConfig.AuthUri;

            var responseType = "code";
            var accessType = "offline";
            var includeGrantedScopes = "true";

            var scope = string.IsNullOrWhiteSpace(_googleConfig.Scopes)
                ? "openid email profile"
                : _googleConfig.Scopes;

            var query = new List<string>
            {
                $"scope={Uri.EscapeDataString(scope)}",
                $"access_type={accessType}",
                $"include_granted_scopes={includeGrantedScopes}",
                $"response_type={responseType}",
                $"redirect_uri={Uri.EscapeDataString(_googleConfig.RedirectUri)}",
                $"client_id={Uri.EscapeDataString(_googleConfig.ClientId)}"
            };

            if (!string.IsNullOrEmpty(state))
            {
                query.Add($"state={Uri.EscapeDataString(state)}");
            }

            var url = $"{authEndpoint}?{string.Join("&", query)}";

            return Task.FromResult(url);
        }

        public async Task<GoogleTokenResponse?> ExchangeCodeForTokenAsync(string code)
        {
            var tokenEndpoint = string.IsNullOrEmpty(_googleConfig.TokenUri)
                ? "https://oauth2.googleapis.com/token"
                : _googleConfig.TokenUri;

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
            
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", _googleConfig.ClientId),
                new KeyValuePair<string, string>("client_secret", _googleConfig.ClientSecret),
                new KeyValuePair<string, string>("redirect_uri", _googleConfig.RedirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            });

            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        }

        public async Task<GoogleUserInfo?> GetGoogleUserAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GoogleUserInfo>();
        }
    }

    public class GoogleTokenInfo
    {
        public string Iss { get; set; } = string.Empty;
        public string Azp { get; set; } = string.Empty;
        public string Aud { get; set; } = string.Empty;
        public string Sub { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string AtHash { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
        public string GivenName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string Locale { get; set; } = string.Empty;
        public long Iat { get; set; }
        public long Exp { get; set; }
    }
}


