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
using InteractionService.Application.DTOs;
using InteractionService.Application.Interfaces;
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using ContentService.Application.Interfaces;
using LearningService.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LearningService.Application.DTOs;
using LearningService.Application.Interfaces;
using LearningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace LearningService.Application.Services
{
    public class StudentProgressService : IStudentProgressService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public StudentProgressService(ICourseRepository courseRepository, IStringLocalizer<SharedResources> localizer)
        {
            _courseRepository = courseRepository;
            _localizer = localizer;
        }

        public async Task<ApiResponse> MarkItemCompletedAsync(MarkItemCompletedDTO dto, string studentId)
        {
            var progress = await _courseRepository.GetProgressAsync(studentId, dto.LectureId, dto.ItemId, dto.ItemType);
            if (progress == null)
            {
                progress = new StudentLectureProgress 
                { 
                    Id = Guid.NewGuid().ToString(), 
                    StudentId = studentId, 
                    LectureId = dto.LectureId, 
                    CourseId = dto.CourseId, 
                    ItemId = dto.ItemId, 
                    ItemType = dto.ItemType, 
                    IsCompleted = true 
                };
                await _courseRepository.AddProgressAsync(progress);
            }
            else
            {
                progress.IsCompleted = true;
                await _courseRepository.UpdateProgressAsync(progress);
            }
            await _courseRepository.SaveChangesAsync();
            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> UnmarkItemCompletedAsync(MarkItemCompletedDTO dto, string studentId)
        {
            var progress = await _courseRepository.GetProgressAsync(studentId, dto.LectureId, dto.ItemId, dto.ItemType);
            if (progress != null)
            {
                progress.IsCompleted = false;
                await _courseRepository.UpdateProgressAsync(progress);
                await _courseRepository.SaveChangesAsync();
            }
            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> GetContinueLearningCoursesAsync(string studentId)
        {
            var enrollments = await _courseRepository.GetEnrollmentsQueryable()
                .AsNoTracking()
                .Include(e => e.Course).ThenInclude(c => c.Instructor)
                .Include(e => e.Course).ThenInclude(c => c.Lectures).ThenInclude(l => l.LectureVideos)
                .Include(e => e.Course).ThenInclude(c => c.Lectures).ThenInclude(l => l.Documents)
                .Include(e => e.Course).ThenInclude(c => c.Lectures).ThenInclude(l => l.Quizzes)
                .Where(e => e.StudentId == studentId && e.Status == true)
                .OrderByDescending(e => e.LastVisit)
                .Take(3)
                .ToListAsync();

            var result = new List<MyCourseDTO>();
            foreach (var e in enrollments)
            {
                var totalLessons = e.Course.Lectures.Sum(l => 
                    (l.LectureVideos?.Count ?? 0) + (l.Documents?.Count ?? 0) + (l.Quizzes?.Count ?? 0));

                var completedLessons = await _courseRepository.GetProgressQueryable()
                    .CountAsync(p => p.StudentId == studentId && p.CourseId == e.CourseId && p.IsCompleted == true);

                var progress = totalLessons > 0 ? (int)Math.Round((double)completedLessons / totalLessons * 100) : 0;

                result.Add(new MyCourseDTO 
                { 
                    Id = e.Course.Id, 
                    Name = e.Course.Name, 
                    ImageUrl = e.Course.ImageUrl, 
                    InstructorName = e.Course.Instructor.FullName,
                    Progress = progress,
                    TotalLessons = totalLessons,
                    CompletedLessons = completedLessons
                });
            }
            
            return new ApiResponse("Success", _localizer["Success"].Value, result, true);
        }
    }
}




