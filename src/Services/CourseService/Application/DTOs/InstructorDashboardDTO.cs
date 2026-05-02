using System;
using System.Collections.Generic;

namespace CourseService.Application.DTOs
{
    public class InstructorDashboardDTO
    {
        public int TotalStudents { get; set; }
        public long TotalRevenue { get; set; }
        public double AverageRating { get; set; }
        public int TotalCourses { get; set; }
        public List<DailyEnrollmentDTO> EnrollmentChart { get; set; } = new();
        public List<RatingDistributionDTO> RatingDistribution { get; set; } = new();
    }

    public class DailyEnrollmentDTO
    {
        public string Date { get; set; } = null!;
        public int Count { get; set; }
    }

    public class RatingDistributionDTO
    {
        public int Star { get; set; }
        public int Count { get; set; }
    }

    public class RecentActivityDTO
    {
        public string Type { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public int? Rating { get; set; }
        public string? QuestionTitle { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
