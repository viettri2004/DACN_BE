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
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ContentService.Application.DTOs
{
    public class CreateQuestionOptionDTO
    {
        [Required]
        public string Content { get; set; } = null!;
        public bool IsCorrect { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class CreateQuestionDTO
    {
        [Required]
        public string Content { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public string? Explanation { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePublicId { get; set; }
        public List<CreateQuestionOptionDTO> Options { get; set; } = new List<CreateQuestionOptionDTO>();
    }

    public class CreateQuizDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        [Required]
        public string LectureId { get; set; } = null!;
        public int TestTime { get; set; }
        // public int AttemptCount { get; set; }
        public List<CreateQuestionDTO> Questions { get; set; } = new List<CreateQuestionDTO>();
    }

    public class UpdateQuizDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? TestTime { get; set; }
        // public int? AttemptCount { get; set; }
        public List<CreateQuestionDTO>? Questions { get; set; }
    }
}


