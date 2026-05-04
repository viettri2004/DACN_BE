using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace CourseService.Application.Services
{
    public class InstructorService : IInstructorService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public InstructorService(ICourseRepository courseRepository, IStringLocalizer<SharedResources> localizer)
        {
            _courseRepository = courseRepository;
            _localizer = localizer;
        }

        public async Task<ApiResponse> GetDashboardAsync(string instructorId)
        {
            var courses = await _courseRepository.GetQueryable()
                .AsNoTracking()
                .Where(c => c.InstructorId == instructorId)
                .Include(c => c.Enrollments).ThenInclude(e => e.Comments)
                .ToListAsync();

            var totalStudents = courses.Sum(c => c.Enrollments.Count);
            var totalRevenue = (long)courses.Sum(c => c.Price * c.Enrollments.Count);
            var ratings = courses.SelectMany(c => c.Enrollments.SelectMany(e => e.Comments)).Where(cm => cm.Type == CommentType.Review).ToList();
            var avgRating = ratings.Any() ? ratings.Average(cm => cm.Rate) : 0;

            // Compute Enrollment Chart for the last 30 days
            var last30Days = Enumerable.Range(0, 30)
                .Select(i => DateTime.UtcNow.Date.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            var enrollmentChart = last30Days.Select(d => new DailyEnrollmentDTO
            {
                Date = d.ToString("yyyy-MM-dd"),
                Count = courses.SelectMany(c => c.Enrollments)
                    .Count(e => e.EnrolledAt.Date == d)
            }).ToList();

            // Compute Rating Distribution (1★ to 5★)
            var ratingDistribution = Enumerable.Range(1, 5).Select(star => new RatingDistributionDTO
            {
                Star = star,
                Count = ratings.Count(cm => cm.Rate == star)
            }).ToList();

            var dashboard = new InstructorDashboardDTO
            {
                TotalStudents = totalStudents,
                TotalRevenue = totalRevenue,
                AverageRating = Math.Round(avgRating, 1),
                TotalCourses = courses.Count,
                EnrollmentChart = enrollmentChart,
                RatingDistribution = ratingDistribution
            };

            return new ApiResponse("Success", _localizer["Success"].Value, dashboard, true);
        }

        public async Task<ApiResponse> GetActivitiesAsync(string instructorId, int page, int pageSize)
        {
            var courseIds = await _courseRepository.GetQueryable()
                .AsNoTracking()
                .Where(c => c.InstructorId == instructorId)
                .Select(c => c.Id)
                .ToListAsync();

            var enrollments = await _courseRepository.GetEnrollmentsQueryable()
                .AsNoTracking()
                .Where(e => courseIds.Contains(e.CourseId))
                .OrderByDescending(e => e.EnrolledAt)
                .Take(pageSize * page)
                .Select(e => new RecentActivityDTO
                {
                    Type = "enrollment",
                    CourseName = e.Course.Name,
                    StudentName = e.Student.FullName ?? "Student",
                    CreatedAt = e.EnrolledAt
                })
                .ToListAsync();

            var ratings = await _courseRepository.GetCommentsQueryable()
                .AsNoTracking()
                .Where(cm => courseIds.Contains(cm.Enrollment.CourseId) && cm.Type == CommentType.Review)
                .OrderByDescending(cm => cm.CreatedAt)
                .Take(pageSize * page)
                .Select(cm => new RecentActivityDTO
                {
                    Type = "rating",
                    CourseName = cm.Enrollment.Course.Name,
                    StudentName = cm.Enrollment.Student.FullName ?? "Student",
                    Rating = cm.Rate,
                    CreatedAt = cm.CreatedAt
                })
                .ToListAsync();

            var qas = await _courseRepository.GetThreadsQueryable()
                .AsNoTracking()
                .Where(t => courseIds.Contains(t.CourseId))
                .OrderByDescending(t => t.CreatedAt)
                .Take(pageSize * page)
                .Select(t => new RecentActivityDTO
                {
                    Type = "qa_question",
                    CourseName = t.Course.Name,
                    StudentName = t.Creator.FullName ?? "Student",
                    QuestionTitle = t.Title,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            var items = enrollments.Concat(ratings).Concat(qas)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalCount = (await _courseRepository.GetEnrollmentsQueryable().AsNoTracking().Where(e => courseIds.Contains(e.CourseId)).CountAsync())
                           + (await _courseRepository.GetCommentsQueryable().AsNoTracking().Where(cm => courseIds.Contains(cm.Enrollment.CourseId) && cm.Type == CommentType.Review).CountAsync())
                           + (await _courseRepository.GetThreadsQueryable().AsNoTracking().Where(t => courseIds.Contains(t.CourseId)).CountAsync());

            return new ApiResponse("Success", _localizer["Success"].Value, new PagedResult<RecentActivityDTO> 
            { 
                Items = items, 
                Page = page, 
                PageSize = pageSize, 
                TotalCount = totalCount 
            }, true);
        }

        public async Task<ApiResponse> GetUnreadThreadsAsync(string instructorId)
        {
            var courseIds = await _courseRepository.GetQueryable()
                .AsNoTracking()
                .Where(c => c.InstructorId == instructorId)
                .Select(c => c.Id)
                .ToListAsync();

            var unreadThreads = await _courseRepository.GetThreadsQueryable()
                .AsNoTracking()
                .Where(t => courseIds.Contains(t.CourseId))
                .Select(t => new
                {
                    t.CourseId,
                    t.Course.Name,
                    t.Course.ImageUrl,
                    t.LastActivityAt,
                    LastMessageUserId = t.Messages.Any() 
                        ? t.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.UserId).FirstOrDefault() 
                        : t.CreatorId
                })
                .ToListAsync();

            var grouped = unreadThreads
                .Where(t => t.LastMessageUserId != instructorId && t.LastMessageUserId != null)
                .GroupBy(t => t.CourseId)
                .Select(g => new UnreadThreadCourseDTO
                {
                    CourseId = g.Key,
                    CourseName = g.First().Name,
                    CourseImage = g.First().ImageUrl,
                    UnreadThreadCount = g.Count(),
                    LastActivityAt = g.Max(t => t.LastActivityAt)
                })
                .ToList();

            return new ApiResponse("Success", _localizer["Success"].Value, grouped, true);
        }
    }
}
