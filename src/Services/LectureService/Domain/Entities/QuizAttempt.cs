using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class QuizAttempt
    {
        public string Id { get; set; } = null!;
        public string EnrollmentId { get; set; } = null!;
        public Enrollment Enrollment { get; set; } = null!;
        public string? QuizId { get; set; }
        public Quiz? Quiz { get; set; }
        public DateTime AttemptedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int Score { get; set; }

        public ICollection<QuizAttemptAnswer> QuizAttemptAnswers { get; set; } = new List<QuizAttemptAnswer>();
    }
}