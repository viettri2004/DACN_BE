using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Domain.Enums;
using src.Shared.Domain.Entities;

namespace CourseService.Application.Interfaces
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
