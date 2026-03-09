using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.DTOs;
using AccountService.Application.Interfaces;
using CourseService.Domain.Enums;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace AccountService.Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public DashboardRepository(AppDbContext context, IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<ApiResponse> GetDashboardDataAsync()
        {
            try
            {
                var currentYear = DateTime.UtcNow.Year;

                // 1. Stats
                var stats = new DashboardStatsDTO
                {
                    TotalStudents = await _context.Set<Student>().CountAsync(),
                    TotalInstructors = await _context.Set<Instructor>().CountAsync(),
                    ApprovedCourses = await _context.Courses.CountAsync(c => c.Status == CourseStatus.Public),
                    PendingCourses = await _context.CourseRequests.CountAsync(r => r.Status == RequestStatus.Pending)
                };

                // 2. User Growth (Current Year)
                var newStudents = await _context.Set<Student>()
                    .Where(u => u.CreatedAt.Year == currentYear)
                    .GroupBy(u => u.CreatedAt.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToListAsync();

                var newInstructors = await _context.Set<Instructor>()
                    .Where(u => u.CreatedAt.Year == currentYear)
                    .GroupBy(u => u.CreatedAt.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToListAsync();

                var userGrowth = new List<UserGrowthChartDTO>();
                for (int i = 1; i <= 12; i++)
                {
                    userGrowth.Add(new UserGrowthChartDTO
                    {
                        Month = i,
                        Year = currentYear,
                        NewStudents = newStudents.FirstOrDefault(x => x.Month == i)?.Count ?? 0,
                        NewInstructors = newInstructors.FirstOrDefault(x => x.Month == i)?.Count ?? 0
                    });
                }

                // 3. Revenue (Current Year)
                var revenueData = await _context.Orders
                    .Where(o => o.PaidAt != null && o.PaidAt.Value.Year == currentYear)
                    .GroupBy(o => o.PaidAt.Value.Month)
                    .Select(g => new { Month = g.Key, Total = g.Sum(x => x.TotalAmount) })
                    .ToListAsync();

                var revenueChart = new List<RevenueChartDTO>();
                for (int i = 1; i <= 12; i++)
                {
                    revenueChart.Add(new RevenueChartDTO
                    {
                        Month = i,
                        Year = currentYear,
                        TotalRevenue = revenueData.FirstOrDefault(x => x.Month == i)?.Total ?? 0
                    });
                }

                // 4. Trending Courses (Top 5 Best Selling)
                var trendingCourses = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .Where(oi => oi.Order.PaidAt != null)
                    .GroupBy(oi => oi.CourseId)
                    .Select(g => new 
                    { 
                        CourseId = g.Key, 
                        Sales = g.Count(),
                        Revenue = g.Sum(x => x.Price) 
                    })
                    .OrderByDescending(x => x.Sales)
                    .Take(5)
                    .ToListAsync();

                var courseIds = trendingCourses.Select(x => x.CourseId).ToList();
                var courses = await _context.Courses.Where(c => courseIds.Contains(c.Id)).Select(c => new {c.Id, c.Name}).ToListAsync();

                var trendingCourseDTOs = trendingCourses.Select(x => new TrendingCourseDTO
                {
                    Id = x.CourseId,
                    Name = courses.FirstOrDefault(c => c.Id == x.CourseId)?.Name ?? "Unknown",
                    SalesCount = x.Sales,
                    Revenue = x.Revenue
                }).ToList();

                // 5. Trending Tags (Top 5)
                var trendingTags = await _context.CourseTags
                    .GroupBy(ct => ct.TagId)
                    .Select(g => new { TagId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();
                
                var tagIds = trendingTags.Select(x => x.TagId).ToList();
                var tags = await _context.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();

                var trendingTagDTOs = trendingTags.Select(x => new TrendingTagDTO
                {
                    Id = x.TagId,
                    Name = tags.FirstOrDefault(t => t.Id == x.TagId)?.Name ?? "Unknown",
                    UsageCount = x.Count
                }).ToList();

                var data = new DashboardDataDTO
                {
                    Stats = stats,
                    UserGrowth = userGrowth,
                    Revenue = revenueChart,
                    TrendingCourses = trendingCourseDTOs,
                    TrendingTags = trendingTagDTOs
                };

                return new ApiResponse("Success", _localizer["Success"].Value, data, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> GetAdminNotificationsAsync()
        {
            try
            {
                var recentInstructorRequests = await _context.Set<InstructorRequest>()
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                var recentCourseRequests = await _context.CourseRequests
                    .Include(r => r.Course)
                    .Include(r => r.Course.Instructor)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                var notifications = new List<AdminNotificationItemDTO>();

                foreach (var ir in recentInstructorRequests)
                {
                    notifications.Add(new AdminNotificationItemDTO
                    {
                        Id = ir.Id.ToString(),
                        Type = "InstructorRequest",
                        // Title = "New Instructor Request",
                        // Message = $"{ir.User.FullName} has requested to become an instructor.",
                        Sender = ir.User.FullName,
                        CourseName = string.Empty,
                        CreatedAt = ir.CreatedAt
                    });
                }

                foreach (var cr in recentCourseRequests)
                {
                    notifications.Add(new AdminNotificationItemDTO
                    {
                        Id = cr.Id,
                        Type = "CourseRequest",
                        // Title = "New Course Approval Request",
                        // Message = $"Instructor {cr.Course.Instructor.FullName} has submitted a new course: {cr.Course.Name}",
                        Sender = cr.Course.Instructor.FullName,
                        CourseName = cr.Course.Name,
                        CreatedAt = cr.CreatedAt
                    });
                }

                var sortedNotifications = notifications
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(10)
                    .ToList();

                var result = new AdminNotificationDTO
                {
                    Notifications = sortedNotifications
                };

                return new ApiResponse("Success", _localizer["Success"].Value, result, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
    }
}