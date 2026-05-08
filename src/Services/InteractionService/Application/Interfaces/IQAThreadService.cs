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
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using InteractionService.Application.Interfaces;
using System.Threading.Tasks;
using InteractionService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace InteractionService.Application.Interfaces
{
    public interface IQAThreadService
    {
        Task<ApiResponse> GetCourseQAThreadsAsync(string courseId, string userId, int pageNumber, int pageSize, string filter = "all");
        Task<ApiResponse> GetThreadMessagesAsync(string threadId, string userId, int pageNumber, int pageSize);
        Task<ApiResponse> CreateQAThreadAsync(CreateThreadDTO createThreadDTO, string userId);
        Task<ApiResponse> AddMessageToThreadAsync(AddMessageDTO addMessageDTO, string userId);
        Task<ApiResponse> UpdateQAThreadAsync(string threadId, UpdateThreadDTO updateThreadDTO, string userId);
        Task<ApiResponse> UpdateQAMessageAsync(string messageId, UpdateMessageDTO updateMessageDTO, string userId);
        Task<ApiResponse> DeleteQAThreadAsync(string threadId, string userId);
        Task<ApiResponse> DeleteQAMessageAsync(string messageId, string userId);
    }
}



