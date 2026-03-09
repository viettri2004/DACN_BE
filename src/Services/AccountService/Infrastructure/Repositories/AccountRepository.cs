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

                Location = "Hồ Chí Minh, Việt Nam",
                BirthDate = new DateTime(2004, 6, 13),
                Gender = "Nam",
                Experience = "Chưa cập nhật",
                MemberSinceYear = 2025,

                Stats = stats
            };

            return new ApiResponse("Success", _localizer["Success"].Value, profileDto, true);
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

        public async Task UpdateInstructorRequestAsync(InstructorRequest request)
        {
            _context.InstructorRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserDiscriminatorToInstructor(string userId)
        {
            // Assuming table name is AspNetUsers and discriminator column is UserType
            // Using parameterized query for safety
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
            return await _context.Users
                .OfType<Instructor>()
                .Cast<User>()
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}