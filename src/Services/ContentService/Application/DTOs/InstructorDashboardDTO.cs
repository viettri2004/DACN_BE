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
using System;
using System.Collections.Generic;

namespace ContentService.Application.DTOs
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


