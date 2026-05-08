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
    public interface IAccountRepository
    {
        Task<User> GetUserFromRefreshToken(string refreshToken);
        Task<RefreshToken?> GetRefreshTokenAsync(string refreshToken);
        Task StoreRefreshTokenAsync(RefreshToken refreshToken);
        Task UpdateRefreshTokenAsync(RefreshToken refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
        Task RevokeAllUserTokensAsync(string userId);
        Task<User> FindUserByEmail(string email);
        Task ChangePassword(User user, ChangePasswordDTO changePasswordDTO);
        Task<ApiResponse> GetUserProfileAsync(string userId);
        Task<ApiResponse> UpdateUserProfileAsync(string userId, UpdateUserProfileDTO dto);
        Task<bool> CreateInstructorRequestAsync(InstructorRequest request);
        Task<List<InstructorRequest>> GetPendingInstructorRequestsAsync();
        Task<InstructorRequest?> GetInstructorRequestByIdAsync(int id);
        Task<InstructorRequest?> GetInstructorRequestByUserIdAsync(string userId);
        Task UpdateInstructorRequestAsync(InstructorRequest request);
        Task UpdateUserDiscriminatorToInstructor(string userId);
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetAllInstructorsAsync();
        Task<User?> GetUserByIdAsync(string userId);
        Task UpdateUserAsync(User user);
    }
}



