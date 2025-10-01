using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace AccountService.Infrastructure.Otp
{
    public class OtpService : IOtpService
    {
        private readonly IDistributedCache _cache; 

        public OtpService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<string> GenerateOtpAsync(string key, TimeSpan ttl)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            await _cache.SetStringAsync(key, otp, options);
            return otp;
        }

        public async Task<bool> ValidateOtpAsync(string key, string otp)
        {
            var storedOtp = await _cache.GetStringAsync(key);
            if (storedOtp == null) return false;

            var isValid = storedOtp == otp;
            if (isValid)
            {
                await _cache.RemoveAsync(key);
            }

            return isValid;
        }
    }
}