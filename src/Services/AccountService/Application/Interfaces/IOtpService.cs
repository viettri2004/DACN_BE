using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Application.Interfaces
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(string key, TimeSpan ttl);
        Task<bool> ValidateOtpAsync(string key, string otp);
    }
}