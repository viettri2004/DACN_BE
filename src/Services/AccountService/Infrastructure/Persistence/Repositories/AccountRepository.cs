using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.DTOs;
using AccountService.Application.Interfaces;
using Data.AppDbContext;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Persistence.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public AccountRepository(AppDbContext context, UserManager<User> userManager)
        {
            _userManager = userManager;
            _context = context;
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
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task ChangePassword(User user, ChangePasswordDTO changePasswordDTO)

        {
            await _userManager.ChangePasswordAsync(user, changePasswordDTO.OldPassword, changePasswordDTO.NewPassword);
        }
    }
}