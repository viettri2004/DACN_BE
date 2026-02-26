using System;

namespace Entities
{
    public class QuizAttemptAnswer
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string QuizAttemptId { get; set; } = null!;
        public QuizAttempt QuizAttempt { get; set; } = null!;
        public string QuestionId { get; set; } = null!;
        public Question Question { get; set; } = null!;
        public string SelectedOptionId { get; set; } = null!;
        public QuestionOption SelectedOption { get; set; } = null!;
    }
}
