using System.Threading.Tasks;
using CourseService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace CourseService.Application.Interfaces
{
    public interface IInstructorService
    {
        Task<ApiResponse> GetDashboardAsync(string instructorId);
        Task<ApiResponse> GetActivitiesAsync(string instructorId, int page, int pageSize);
        Task<ApiResponse> GetUnreadThreadsAsync(string instructorId);
    }
}
