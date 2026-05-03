using System.Threading.Tasks;
using CourseService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace CourseService.Application.Interfaces
{
    public interface IStudentProgressService
    {
        Task<ApiResponse> MarkItemCompletedAsync(MarkItemCompletedDTO dto, string studentId);
        Task<ApiResponse> UnmarkItemCompletedAsync(MarkItemCompletedDTO dto, string studentId);
        Task<ApiResponse> GetContinueLearningCoursesAsync(string studentId);
    }
}
