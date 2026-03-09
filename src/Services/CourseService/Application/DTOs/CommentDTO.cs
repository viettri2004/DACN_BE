using System;
using System.Collections.Generic;

namespace CourseService.Application.DTOs
{
    public class CommentDTO
    {
        public string CommentId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public int Rate { get; set; }
        public string Content { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public List<CommentDTO> Replies { get; set; } = new List<CommentDTO>();
    }

    public class AddCommentDTO
    {
        public string CourseId { get; set; } = null!;
        public int Rate { get; set; }
        public string Content { get; set; } = null!;
    }

    public class ReplyCommentDTO
    {
        public string ParentCommentId { get; set; } = null!;
        public string Content { get; set; } = null!;
    }
}
