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
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using SearchService.Application.Services;
using SearchService.Application.Interfaces;
using System;
using System.Threading.Tasks;
using SearchService.Application.DTOs;
using Shared.Domain.Entities;
using src.Shared.Domain.Entities;

namespace SearchService.Application.Interfaces
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



