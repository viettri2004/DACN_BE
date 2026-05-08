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
using System.Linq;
using System.Threading.Tasks;

namespace ContentService.Application.DTOs
{
    public class CourseDetailDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string InstructorName { get; set; } = null!;
        public string? InstructorAvatar { get; set; }
        public string? InstructorIntro { get; set; }
        public string InstructorJobPosition { get; set; } = null!;
        public int InstructorTotalCourses { get; set; }
        public double Rating { get; set; }
        public int TotalReviews { get; set; }
        public int TotalStudents { get; set; }
        public double TotalHours { get; set; }
        public int TotalLessons { get; set; }
        public bool IsEnrolled { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime LastUpdate { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<CurriculumSectionDTO> Curriculum { get; set; } = new List<CurriculumSectionDTO>();
        public List<LecturePreviewDTO> Lectures { get; set; } = new List<LecturePreviewDTO>();
    }

    public class CurriculumSectionDTO
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public List<CurriculumLessonDTO> Lessons { get; set; } = new List<CurriculumLessonDTO>();
    }

    public class CurriculumLessonDTO
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Duration { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsPreview { get; set; }
    }

    public class LecturePreviewDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public List<VideoPreviewDTO> Videos { get; set; } = new List<VideoPreviewDTO>();
        public List<QuizPreviewDTO> Quizzes { get; set; } = new List<QuizPreviewDTO>();
        public List<DocumentPreviewDTO> Documents { get; set; } = new List<DocumentPreviewDTO>();
    }

    public class VideoPreviewDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public double Duration { get; set; }
        public int DisplayOrder { get; set; }
        public string? VideoUrl { get; set; }
        public bool IsTrial { get; set; }
    }

    public class QuizPreviewDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class DocumentPreviewDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}


