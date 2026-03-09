using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class Comment
    {
        public string Id { get; set; } = null!;

        public string Content { get; set; } = null!;
        [Range(1, 5)]
        public int Rate { get; set; } = 5;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string EnrollmentId { get; set; } = null!;
        public Enrollment Enrollment { get; set; } = null!;
        public string? ReplyId { get; set; }
        public Comment? Parent { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}