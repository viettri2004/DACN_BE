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

namespace AccountService.Infrastructure.Persistence.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public AccountRepository(AppDbContext context, UserManager<User> userManager, IStringLocalizer<SharedResources> localizer)
        {
            _userManager = userManager;
            _context = context;
            _localizer = localizer;
        }
        
        public async Task<User> GetUserFromRefreshToken(string refreshToken)
        {
            return await _context.Users
                .Join(_context.UserTokens,
                    u => u.Id,
                    t => t.UserId,
                    (u, t) => new { User = u, Token = t })
                .Where(x => x.Token.Name == "RefreshToken" && x.Token.Value == refreshToken)
                .Select(x => x.User)
                .FirstOrDefaultAsync();
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
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);
            }

            var userComments = await _context.Comments
                .Where(c => c.Enrollment.StudentId == userId && c.Rate > 0)
                .ToListAsync();

            var stats = new UserLearningStatsDTO
            {
                CompletionProgress = 36,    
                TotalHours = 36,            
                TotalCertificates = 36,        
                CurrentStreak = 36,            
                AverageGivenRating = 36
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

                Location = "Hồ Chí Minh, Việt Nam",
                BirthDate = new DateTime(1363, 6, 03),
                Gender = "Nam",
                Experience = "Chưa cập nhật",
                MemberSinceYear = 2036,

                Stats = stats
            };

            return new ApiResponse("Success", _localizer["Success"].Value, profileDto, true);
        }
    }
}