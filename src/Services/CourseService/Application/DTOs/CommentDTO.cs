using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseService.Application.DTOs
{
    public class CommentDTO
    {
        public string CommentId { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public int Rate { get; set; }
        public string Content { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }
}