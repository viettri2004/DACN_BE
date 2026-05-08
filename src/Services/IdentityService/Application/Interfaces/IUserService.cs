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
using System.Threading.Tasks;
using IdentityService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace IdentityService.Application.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse> GetUserProfileAsync(string userId);
        Task<ApiResponse> UpdateUserProfileAsync(string userId, UpdateUserProfileDTO dto);
        Task<ApiResponse> GetPendingInstructorRequestsAsync();
        Task<ApiResponse> ApproveInstructorRequestAsync(int requestId);
        Task<ApiResponse> RejectInstructorRequestAsync(int requestId, string reason);
    }
}



