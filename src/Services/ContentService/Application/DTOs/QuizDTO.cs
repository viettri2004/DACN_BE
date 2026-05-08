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

namespace ContentService.Application.DTOs
{
    public class QuestionOptionDTO
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsCorrect { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class QuestionDTO
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public string? Explanation { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePublicId { get; set; }
        public List<QuestionOptionDTO> Options { get; set; } = new List<QuestionOptionDTO>();
    }

    public class QuizDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string LectureId { get; set; } = null!;
        public int TestTime { get; set; }
        public int AttemptCount { get; set; }
        public List<QuestionDTO> Questions { get; set; } = new List<QuestionDTO>();
    }
}


