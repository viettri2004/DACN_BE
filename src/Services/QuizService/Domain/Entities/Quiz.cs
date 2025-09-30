using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class Quiz
    {
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public int NumberId { get; set; }

        public int TestTime { get; set; } = 0;
        public int AttemptCount { get; set; } = 0;

        public ICollection<Questionnaire> Questionnaires { get; set; } = new List<Questionnaire>();
    }
}