using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.DTOs;
using AccountService.Application.Interfaces;
using Data.Context;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using src.Shared.Infrastructure;
using Shared.Infrastructure.cloudinaryService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace AccountService.Infrastructure.Persistence.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IDistributedCache _cache;
        private readonly CloudinaryService _cloudinaryService;

        public AccountRepository(AppDbContext context, 
                                UserManager<User> userManager, 
                                IStringLocalizer<SharedResources> localizer, 
                                IDistributedCache cache,
                                CloudinaryService cloudinaryService)
        {
            _userManager = userManager;
            _context = context;
            _localizer = localizer;
            _cache = cache;
            _cloudinaryService = cloudinaryService;
        }
        
        public async Task<User> GetUserFromRefreshToken(string refreshToken)
        {
            return await _context.RefreshTokens
                .Where(t => t.Token == refreshToken && !t.IsRevoked)
                .Select(t => t.User)
                .FirstOrDefaultAsync();
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string refreshToken)
        {
            return await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == refreshToken);
        }

        public async Task StoreRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (token != null)
            {
                token.IsRevoked = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeAllUserTokensAsync(string userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<User> FindUserByEmail(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task ChangePassword(User user, ChangePasswordDTO changePasswordDTO)
        {
            await _userManager.ChangePasswordAsync(user, changePasswordDTO.OldPassword, changePasswordDTO.NewPassword);
        }

        public async Task<ApiResponse> GetUserProfileAsync(string userId)
        {
            string cacheKey = $"user:profile:{userId}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<ApiResponse>(cachedData, JsonSettings.CamelCase);
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);
            }

            var stats = new UserLearningStatsDTO
            {
                CompletionProgress = 0,    
                TotalHours = 0,            
                TotalCertificates = 0,        
                CurrentStreak = 0,            
                AverageGivenRating = 0
            };

            var profileDto = new UserProfileDTO
            {
                Username = user.UserName ?? "",
                FullName = user.FullName,
                Email = user.Email ?? "",
                JobPosition = user.JobPosition ?? "",
                Organization = user.Organization ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Description = user.Description ?? "",
                AvatarUrl = user.AvatarUrl ?? "",
                MemberSinceYear = user.CreatedAt.Year,
                Stats = stats
            };

            var response = new ApiResponse("Success", _localizer["Success"].Value, profileDto, true);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response, JsonSettings.CamelCase), cacheOptions);

            return response;
        }

        public async Task<ApiResponse> UpdateUserProfileAsync(string userId, UpdateUserProfileDTO dto)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);

                if (!string.IsNullOrEmpty(dto.FullName)) user.FullName = dto.FullName;
                if (!string.IsNullOrEmpty(dto.JobPosition)) user.JobPosition = dto.JobPosition;
                if (!string.IsNullOrEmpty(dto.Organization)) user.Organization = dto.Organization;
                if (!string.IsNullOrEmpty(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;
                if (!string.IsNullOrEmpty(dto.Description)) user.Description = dto.Description;

                if (!string.IsNullOrEmpty(dto.AvatarUrl) && !string.IsNullOrEmpty(dto.AvatarPublicId))
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(user.AvatarPublicId))
                    {
                        await _cloudinaryService.DeleteImageAsync(user.AvatarPublicId);
                    }

                    user.AvatarUrl = dto.AvatarUrl;
                    user.AvatarPublicId = dto.AvatarPublicId;
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                await RemoveProfileCache(userId);

                return new ApiResponse("Success", _localizer["ProfileUpdated"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<bool> CreateInstructorRequestAsync(InstructorRequest request)
        {
            _context.InstructorRequests.Add(request);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<InstructorRequest>> GetPendingInstructorRequestsAsync()
        {
            return await _context.InstructorRequests
                .Include(r => r.User)
                .Where(r => r.Status == "Pending")
                .ToListAsync();
        }

        public async Task<InstructorRequest?> GetInstructorRequestByIdAsync(int id)
        {
            return await _context.InstructorRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<InstructorRequest?> GetInstructorRequestByUserIdAsync(string userId)
        {
            return await _context.InstructorRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == "Pending");
        }

        public async Task UpdateInstructorRequestAsync(InstructorRequest request)
        {
            _context.InstructorRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserDiscriminatorToInstructor(string userId)
        {
            var sql = "UPDATE \"AspNetUsers\" SET \"UserType\" = 'Instructor' WHERE \"Id\" = {0}";
            await _context.Database.ExecuteSqlRawAsync(sql, userId);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<User>> GetAllInstructorsAsync()
        {
            string cacheKey = "users:instructors";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<List<User>>(cachedData, JsonSettings.CamelCase);
            }

            var instructors = await _context.Users
                .OfType<Instructor>()
                .Cast<User>()
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
            };
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(instructors, JsonSettings.CamelCase), cacheOptions);

            return instructors;
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            await RemoveProfileCache(user.Id);
            await _cache.RemoveAsync("users:instructors");
        }

        private async Task RemoveProfileCache(string userId)
        {
            await _cache.RemoveAsync($"user:profile:{userId}");
        }
    }
}
