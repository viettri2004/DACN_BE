using System;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using Entities; 
using Shared.Domain.Entities;
using src.Shared.Domain.Entities;

namespace CourseService.Application.Interfaces
{
    
    public interface ILuceneSearchService : IDisposable
    {
        Task<ApiResponse> SearchCoursesAsync(CourseSearchDTO searchParameters, string studentId);
        Task<ApiResponse> SearchCoursesPreviewAsync(string searchTerm, string studentId);
        Task IndexCourseAsync(Course course);
        Task IndexCourseAsync(string courseId);
        Task DeleteCourseFromIndexAsync(string courseId);
        Task IndexAllCoursesAsync();
    }
}