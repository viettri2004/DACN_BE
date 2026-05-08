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
    public class CourseContentDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public IEnumerable<string> Tags { get; set; } = new List<string>();
        public int Progress { get; set; }
        public int TotalSections { get; set; }
        public double TotalHours { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public double TotalStudyTime { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ImageUrl { get; set; }
        public string? Status { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public double Rating { get; set; }
        public IEnumerable<LectureContentDTO> Lectures { get; set; } = new List<LectureContentDTO>();
    }

    public class LectureContentDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsCompleted { get; set; }
        public IEnumerable<VideoContentDTO> Videos { get; set; } = new List<VideoContentDTO>();
        public IEnumerable<DocumentContentDTO> Documents { get; set; } = new List<DocumentContentDTO>();
        public IEnumerable<QuizContentDTO> Quizzes { get; set; } = new List<QuizContentDTO>();
    }
    public class VideoContentDTO
    {
        public string Id { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public string Name { get; set; } = null!;
        public double Duration { get; set; }
        public bool IsCompleted { get; set; }
    }
    public class DocumentContentDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Url { get; set; }
        public bool IsCompleted { get; set; }
    }
    public class QuizContentDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsCompleted { get; set; }
    }
}


