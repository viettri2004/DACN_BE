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
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using System.Collections.Generic;

namespace IdentityService.Application.DTOs
{
    public class DashboardStatsDTO
    {
        public int TotalStudents { get; set; }
        public int TotalInstructors { get; set; }
        public int ApprovedCourses { get; set; }
        public int PendingCourses { get; set; }
    }

    public class UserGrowthChartDTO
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int NewStudents { get; set; }
        public int NewInstructors { get; set; }
    }

    public class RevenueChartDTO
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class TrendingCourseDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TrendingTagDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int UsageCount { get; set; }
    }

    public class DashboardDataDTO
    {
        public DashboardStatsDTO Stats { get; set; } = new();
        public List<UserGrowthChartDTO> UserGrowth { get; set; } = new();
        public List<RevenueChartDTO> Revenue { get; set; } = new();
        public List<TrendingCourseDTO> TrendingCourses { get; set; } = new();
        public List<TrendingTagDTO> TrendingTags { get; set; } = new();
    }
}


