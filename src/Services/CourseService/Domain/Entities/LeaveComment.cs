using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class LeaveComment
    {
        [Key]
        public int CommentId { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        [Range(1,5)]
        public int Rate { get; set; } = 5;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string Content { get; set; } = string.Empty;
    }
}