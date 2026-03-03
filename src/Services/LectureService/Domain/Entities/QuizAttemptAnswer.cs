using System;

namespace Entities
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
