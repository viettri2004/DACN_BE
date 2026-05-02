using System.Threading.Tasks;
using CourseService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace CourseService.Application.Interfaces
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
