using System;
using System.Collections.Generic;

namespace Entities
{
    public class QuestionAnswer
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? Title { get; set; } 
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public string CourseId { get; set; } = null!;
        public Course Course { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;

        public string? ParentId { get; set; }
        public QuestionAnswer? Parent { get; set; }
        public ICollection<QuestionAnswer> Replies { get; set; } = new List<QuestionAnswer>();
    }
}
