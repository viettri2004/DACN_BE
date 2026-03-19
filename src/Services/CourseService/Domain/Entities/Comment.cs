using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Domain.Enums;

namespace Entities
{
    public class Comment
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int Rate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public CommentType Type { get; set; }
        public string EnrollmentId { get; set; } = null!;
        public Enrollment Enrollment { get; set; } = null!;
        public string? ReplyId { get; set; }
        public Comment? Parent { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}