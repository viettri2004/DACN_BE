using System;
using System.Collections.Generic;

namespace LectureService.Application.DTOs
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
