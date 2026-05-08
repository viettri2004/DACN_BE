using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using OrderingService.Domain.Entities;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using LearningService.Application.Services;
using LearningService.Application.Interfaces;
using LearningService.Domain.Entities;
using InteractionService.Application.DTOs;
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using InteractionService.Application.Interfaces;
using System.Threading.Tasks;
using src.Shared.Domain.Entities;

namespace InteractionService.Application.Interfaces
{
    public interface IWishlistService
    {
        Task<ApiResponse> AddToWishlistAsync(string courseId, string studentId);
        Task<ApiResponse> RemoveFromWishlistAsync(string courseId, string studentId);
        Task<ApiResponse> GetStudentWishlistAsync(string studentId, int pageNumber, int pageSize);
    }
}



