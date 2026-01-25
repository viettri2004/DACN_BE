using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class Quiz
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string LectureId { get; set; } = null!;

        public Lecture Lecture { get; set; } = null!;

        public int TestTime { get; set; } = 0;
        public int AttemptCount { get; set; }   = 0;

        public ICollection<Questionnaire> Questionnaires { get; set; } = new List<Questionnaire>();
        public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    }
}