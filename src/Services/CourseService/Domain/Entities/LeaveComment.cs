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
        public string CommentId { get; set; } = null!;

        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;

        public string CourseId { get; set; } = null!;
        public Course Course { get; set; } = null!;

        [Range(1,5)]
        public int Rate { get; set; } = 5;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string Content { get; set; } = string.Empty;
    }
}