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
        Task<ApiResponse> Register(RegisterDTO registerDTO);
        Task<(ApiResponse response, string refreshToken)> LoginAsync(LoginDTO loginDTO);
        Task<(ApiResponse response, string refreshToken)> GoogleLoginAsync(string IdToken);
        Task<(ApiResponse response, string refreshToken)> RefreshToken(string refreshToken);
        Task<ApiResponse> ResetPassword(string email, string newPassword);
        Task<(ApiResponse response, string refreshToken, string redirectUrl)> GoogleCallbackAsync(string code, string? state, string? savedState);
        Task<ApiResponse> RequestInstructor(string userId, InstructorRequestDTO requestDTO);
        Task<ApiResponse> GetInstructorRequests();
        Task<ApiResponse> ApproveInstructorRequest(int requestId, string adminId, bool isApproved);
        Task<ApiResponse> LogoutAsync(string userId);
        Task<ApiResponse> GetAllUsersAsync();
        Task<ApiResponse> GetAllInstructorsAsync();
        Task<ApiResponse> BanUserAsync(BanUserDTO dto);
    }
}