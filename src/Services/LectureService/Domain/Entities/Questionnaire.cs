using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class Questionnaire
    {
        public string QuizId { get; set; } = null!;
        public int QuestionNumber { get; set; }
        public string Question { get; set; } = null!;
        public string Key { get; set; } = null!;
        public string? Description { get; set; }
        public Quiz Quiz { get; set; } = null!;
    }
}