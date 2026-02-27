using System;
using System.Collections.Generic;

namespace LectureService.Application.DTOs
{
    public class QuizAttemptRequestDTO
    {
        public string QuizId { get; set; } = null!;
    }

    public class QuizAttemptResponseDTO
    {
        public string AttemptId { get; set; } = null!;
        public string QuizId { get; set; } = null!;
        public string QuizName { get; set; } = null!;
        public int TestTime { get; set; }
        public List<QuestionDTO> Questions { get; set; } = new List<QuestionDTO>();
    }

    public class QuizAttemptSummaryDTO
    {
        public string Id { get; set; } = null!;
        public DateTime AttemptedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int Score { get; set; }
    }
}
