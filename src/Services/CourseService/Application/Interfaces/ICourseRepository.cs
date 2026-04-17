using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace CourseService.Application.Interfaces
{
    public interface ICourseRepository
    {
        Task<ApiResponse> CreateCourseAsync(CreateCourseDTO createCourseDTO, string instructorId);
        Task<ApiResponse> UpdateCourseAsync(string courseId, UpdateCourseDTO updateCourseDTO, string instructorId);
        Task<ApiResponse> GetCourseDetailAsync(string courseId, string studentId);
        Task<ApiResponse> GetCourseCommentsAsync(string courseId, string? userId);
        Task<ApiResponse> GetRecommendedCoursesAsync();
        Task<ApiResponse> GetCoursesByStudentIdAsync(string instructorId);
        Task<ApiResponse> GetCoursesAsync(CourseQueryParameters queryParams, string studentId);
        Task<ApiResponse> GetCourseContentAsync(string courseId, string userId);
        Task<ApiResponse> GetCoursesByInstructorAsync(string instructorId);
        Task<ApiResponse> DeleteCourseAsync(string courseId, string instructorId);
        Task<ApiResponse> CreateCourseRequestAsync(string courseId, string instructorId);
        Task<ApiResponse> GetPendingCourseRequestsAsync();
        Task<ApiResponse> ApproveCourseRequestAsync(string requestId, ResponseRequestDTO responseRequestDTO);
        Task<ApiResponse> RejectCourseRequestAsync(string requestId, ResponseRequestDTO responseRequestDTO);
        Task<ApiResponse> GetAllCoursesForAdminAsync();
        Task<ApiResponse> AddCommentAsync(AddCommentDTO addCommentDTO, string userId);
        Task<ApiResponse> UpdateCommentAsync(string commentId, UpdateCommentDTO updateCommentDTO, string userId);
        Task<ApiResponse> DeleteCommentAsync(string commentId, string userId);
        Task<ApiResponse> ReplyToCommentAsync(AddReplyCommentDTO replyDTO, string userId);
        Task<ApiResponse> MarkLectureCompletedAsync(string lectureId, string studentId);
    }
}
