using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities;
using src.Shared.Domain.Entities;

namespace AccountService.Application.Interfaces
{
    public interface ITokenService
    {
        Task<string> GenerateAccessTokenAsync(User user);
        string GenerateRefreshToken();
        Task StoreRefreshTokenAsync(User user, string refreshToken);
        Task<string?> GetRefreshTokenAsync(User user);
        Task RemoveRefreshTokenAsync(User user);
    }
}