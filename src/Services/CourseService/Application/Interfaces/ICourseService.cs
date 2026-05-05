using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Domain.Enums;
using src.Shared.Domain.Entities;

namespace CourseService.Application.Interfaces
{
    public interface ICourseService
    {
        Task<ApiResponse> CreateCourseAsync(CreateCourseDTO createCourseDTO, string instructorId);
        Task<ApiResponse> UpdateCourseAsync(string courseId, UpdateCourseDTO updateCourseDTO, string instructorId);
        Task<ApiResponse> GetCourseDetailAsync(string courseId, string studentId);
        Task<ApiResponse> GetRecommendedCoursesAsync(string? userId, int pageNumber, int pageSize);
        Task<ApiResponse> GetCoursesByStudentIdAsync(string studentId, int pageNumber, int pageSize, string? filterStatus = "All");
        Task<ApiResponse> GetCourseContentAsync(string courseId, string userId);
        Task<ApiResponse> GetInstructorCourseContentAsync(string courseId, string instructorId);
        Task<ApiResponse> GetCoursesByInstructorAsync(string instructorId, int pageNumber, int pageSize);
        Task<ApiResponse> DeleteCourseAsync(string courseId, string instructorId);
        Task<ApiResponse> CreateCourseRequestAsync(string courseId, string instructorId);
        Task<ApiResponse> GetPendingCourseRequestsAsync(int pageNumber, int pageSize);
        Task<ApiResponse> ApproveCourseRequestAsync(string requestId, ResponseRequestDTO responseRequestDTO);
        Task<ApiResponse> RejectCourseRequestAsync(string requestId, ResponseRequestDTO responseRequestDTO);
        Task<ApiResponse> GetAllCoursesForAdminAsync(int pageNumber, int pageSize);
    }
}
