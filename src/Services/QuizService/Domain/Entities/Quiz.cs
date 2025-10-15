using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class Quiz
    {
        public string Id { get; set; } = null!;
        public string CourseId { get; set; } = null!;
        public Course Course { get; set; } = null!;

        public string NumberId { get; set; } = null!;

        public int TestTime { get; set; } = 0;
        public int AttemptCount { get; set; }   = 0;

        public ICollection<Questionnaire> Questionnaires { get; set; } = new List<Questionnaire>();
        public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    }
}