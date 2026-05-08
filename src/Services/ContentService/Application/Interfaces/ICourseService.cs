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
using InteractionService.Application.Interfaces;
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using System.Threading.Tasks;
using ContentService.Application.DTOs;
using ContentService.Domain.Enums;
using src.Shared.Domain.Entities;

namespace ContentService.Application.Interfaces
{
    public interface ICourseService
    {
        Task<ApiResponse> CreateCourseAsync(CreateCourseDTO createCourseDTO, string instructorId);
        Task<ApiResponse> UpdateCourseAsync(string courseId, UpdateCourseDTO updateCourseDTO, string instructorId);
        Task<ApiResponse> GetCourseDetailAsync(string courseId, string studentId);
        Task<ApiResponse> GetRecommendedCoursesAsync(string? userId, int pageNumber, int pageSize);
        Task<ApiResponse> GetCoursesByStudentIdAsync(string studentId, int pageNumber, int pageSize, string? filterStatus = "All");
        Task<ApiResponse> GetCourseContentAsync(string courseId, string userId, bool isAdmin = false);
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


