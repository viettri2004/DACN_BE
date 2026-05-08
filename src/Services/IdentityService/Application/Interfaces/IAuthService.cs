using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using OrderingService.Domain.Entities;
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
using IdentityService.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace IdentityService.Application.Interfaces
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
        Task<ApiResponse> ApproveInstructorRequest(ApproveRequestDTO dto, string adminId);
        Task<ApiResponse> LogoutAsync(string? refreshToken);
        Task<ApiResponse> GlobalLogoutAsync(string userId);
        Task<ApiResponse> ChangePasswordAsync(string userId, ChangePasswordDTO dto);
        Task<ApiResponse> GetAllUsersAsync();
        Task<ApiResponse> GetAllInstructorsAsync();
        Task<ApiResponse> BanUserAsync(BanUserDTO dto);
    }
}



