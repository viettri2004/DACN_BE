using System.Collections.Generic;

namespace AccountService.Application.DTOs
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