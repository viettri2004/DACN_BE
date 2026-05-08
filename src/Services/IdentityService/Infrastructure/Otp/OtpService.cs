using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using OrderingService.Domain.Entities;
using IdentityService.Application.DTOs;
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
using System.Threading.Tasks;
using IdentityService.Application.Interfaces;
using StackExchange.Redis;

namespace IdentityService.Infrastructure.Otp
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


