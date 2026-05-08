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
    public class QuizAttemptAnswerResultDTO
    {
        public string QuestionId { get; set; } = null!;
        public string SelectedOptionId { get; set; } = null!;
        public string CorrectOptionId { get; set; } = null!;
        public bool IsCorrect { get; set; }
        public string? Explanation { get; set; }
    }

    public class QuizResultDTO
    {
        public string QuizAttemptId { get; set; } = null!;
        public string QuizId { get; set; } = null!;
        public string QuizName { get; set; } = null!;
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswersCount { get; set; }
        public DateTime AttemptedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<QuizAttemptAnswerResultDTO> DetailedResults { get; set; } = new List<QuizAttemptAnswerResultDTO>();
    }
}


