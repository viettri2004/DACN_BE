using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace AccountService.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse> Register(RegisterDTO model);
        // Task<ApiResponse> Login(LoginDTO model);
        // Task<ApiResponse> RefreshToken(string refreshToken);
        // Task<ApiResponse> ResetPassword(string email, string newPassword);
        // Task<ApiResponse> ChangePassword(int userId, ChangePasswordDTO dto);
    }
}