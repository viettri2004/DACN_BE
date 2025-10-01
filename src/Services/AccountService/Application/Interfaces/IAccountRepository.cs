using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities;

namespace AccountService.Application.Interfaces
{
    public interface IAccountRepository
    {
        Task<User> GetUserFromRefreshToken(string refreshToken);
        Task<User> FindUserByEmail(string email);
    }
}