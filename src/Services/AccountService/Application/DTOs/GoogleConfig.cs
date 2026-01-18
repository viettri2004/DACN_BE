using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Application.DTOs
{
    public class GoogleConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string AuthUri { get; set; } = string.Empty;
        public string TokenUri { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string Scopes { get; set; } = "openid email profile";
        public string FrontendSuccessUrl { get; set; } = string.Empty;
        public string FrontendFailUrl { get; set; } = string.Empty;
    }
}
