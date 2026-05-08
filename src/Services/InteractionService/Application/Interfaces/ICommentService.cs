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
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using InteractionService.Application.Interfaces;
using System.Threading.Tasks;
using InteractionService.Application.DTOs;
using InteractionService.Domain.Enums;
using src.Shared.Domain.Entities;

namespace InteractionService.Application.Interfaces
{
    public interface ICommentService
    {
        Task<ApiResponse> GetCourseCommentsAsync(string courseId, string? userId, CommentType type, int pageNumber, int pageSize, int? rating = null);
        Task<ApiResponse> AddCommentAsync(AddCommentDTO addCommentDTO, string userId);
        Task<ApiResponse> UpdateCommentAsync(string commentId, UpdateCommentDTO updateCommentDTO, string userId);
        Task<ApiResponse> DeleteCommentAsync(string commentId, string userId);
        Task<ApiResponse> ReplyToCommentAsync(AddReplyCommentDTO replyDTO, string userId);
    }
}




