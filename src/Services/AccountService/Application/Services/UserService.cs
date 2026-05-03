using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.DTOs;
using AccountService.Application.Interfaces;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Shared.Infrastructure.cloudinaryService;
using src.Shared.Domain.Entities;
using src.Shared.Infrastructure;
using src.Shared.Resources;

namespace AccountService.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IDistributedCache _cache;
        private readonly CloudinaryService _cloudinaryService;
        private readonly UserManager<User> _userManager;

        public UserService(IAccountRepository accountRepository, 
                           IStringLocalizer<SharedResources> localizer, 
                           IDistributedCache cache, 
                           CloudinaryService cloudinaryService,
                           UserManager<User> userManager)
        {
            _accountRepository = accountRepository;
            _localizer = localizer;
            _cache = cache;
            _cloudinaryService = cloudinaryService;
            _userManager = userManager;
        }

        public async Task<ApiResponse> GetUserProfileAsync(string userId)
        {
            string cacheKey = $"user:profile:{userId}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<ApiResponse>(cachedData, JsonSettings.CamelCase)!;
            }

            var user = await _accountRepository.GetUserByIdAsync(userId);
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
                var user = await _accountRepository.GetUserByIdAsync(userId);
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
                        try 
                        {
                            await _cloudinaryService.DeleteImageAsync(user.AvatarPublicId);
                        }
                        catch { }
                    }

                    user.AvatarUrl = dto.AvatarUrl;
                    user.AvatarPublicId = dto.AvatarPublicId;
                }

                await _accountRepository.UpdateUserAsync(user);
                await RemoveProfileCache(userId);

                return new ApiResponse("Success", _localizer["ProfileUpdated"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> GetPendingInstructorRequestsAsync()
        {
            var requests = await _accountRepository.GetPendingInstructorRequestsAsync();
            var dtos = requests.Select(r => new 
            {
                r.Id,
                r.UserId,
                UserName = r.User?.FullName,
                JobPosition = r.User?.JobPosition,
                Organization = r.User?.Organization,
                r.Experience,
                CertificateUrl = r.Certificate,
                RequestedAt = r.CreatedAt,
                r.Status
            });

            return new ApiResponse("Success", _localizer["Success"].Value, dtos, true);
        }

        public async Task<ApiResponse> ApproveInstructorRequestAsync(int requestId)
        {
            var request = await _accountRepository.GetInstructorRequestByIdAsync(requestId);
            if (request == null)
                return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            if (request.Status != "Pending")
                return new ApiResponse("Error", _localizer["Error"].Value, null, false);

            request.Status = "Approved";
            await _accountRepository.UpdateInstructorRequestAsync(request);

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user != null)
            {
                await _userManager.AddToRoleAsync(user, "Instructor");
                await _accountRepository.UpdateUserDiscriminatorToInstructor(user.Id);
                await RemoveProfileCache(user.Id);
            }

            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> RejectInstructorRequestAsync(int requestId, string reason)
        {
            var request = await _accountRepository.GetInstructorRequestByIdAsync(requestId);
            if (request == null)
                return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            if (request.Status != "Pending")
                return new ApiResponse("Error", _localizer["Error"].Value, null, false);

            request.Status = "Rejected";
            await _accountRepository.UpdateInstructorRequestAsync(request);

            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        private async Task RemoveProfileCache(string userId)
        {
            await _cache.RemoveAsync($"user:profile:{userId}");
        }
    }
}
