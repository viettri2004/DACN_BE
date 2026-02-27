using System;
using System.Collections.Generic;

namespace Entities
{
    public class Question
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = null!; 
        public int DisplayOrder { get; set; } 
        public string? Explanation { get; set; }
        
        public string QuizId { get; set; } = null!;
        public Quiz Quiz { get; set; } = null!;

        public ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();
    }
}
