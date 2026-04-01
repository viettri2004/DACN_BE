using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.Interfaces;
using StackExchange.Redis;

namespace AccountService.Infrastructure.Otp
{
    public class OtpService : IOtpService
    {
        private readonly IDatabase _db;

        public OtpService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<string> GenerateOtpAsync(string key, TimeSpan ttl)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            await _db.StringSetAsync(key, otp, ttl);
            return otp;
        }

        public async Task<bool> ValidateOtpAsync(string key, string otp)
        {
            var storedOtp = await _db.StringGetAsync(key);
            if (storedOtp.IsNull) return false;

            var isValid = storedOtp == otp;
            if (isValid)
            {
                await _db.KeyDeleteAsync(key);
            }

            return isValid;
        }
    }
}