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

namespace LearningService.Domain.Entities
{
    public class QuizAttemptAnswer
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string QuizAttemptId { get; set; } = null!;
        public QuizAttempt QuizAttempt { get; set; } = null!;
        public string? QuestionId { get; set; }
        public Question? Question { get; set; }
        public string? SelectedOptionId { get; set; }
        public QuestionOption? SelectedOption { get; set; }
    }
}


