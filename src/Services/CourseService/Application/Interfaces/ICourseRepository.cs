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
        Task<ApiResponse> GetCourseDetailAsync(string courseId);
        Task<ApiResponse> GetCourseCommentsAsync(string courseId);
        Task<ApiResponse> GetRecommendedCoursesAsync();
        Task<ApiResponse> GetCoursesByStudentIdAsync(string instructorId);
        Task<ApiResponse> GetCoursesAsync(CourseQueryParameters queryParams);
    }
}