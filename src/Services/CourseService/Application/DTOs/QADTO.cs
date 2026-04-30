using System;
using System.Collections.Generic;

namespace CourseService.Application.DTOs
{
    public class QAThreadDTO
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string CreatorName { get; set; } = null!;
        public string? CreatorAvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public bool IsMyThread { get; set; }
        public List<QAMessageDTO> Messages { get; set; } = new List<QAMessageDTO>();
    }

    public class QAMessageDTO
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsMyMessage { get; set; }
        public bool IsInstructor { get; set; }
    }

    public class CreateThreadDTO
    {
        public string CourseId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!; // First message content
    }

    public class AddMessageDTO
    {
        public string ThreadId { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    public class UpdateThreadDTO
    {
        public string Title { get; set; } = null!;
    }

    public class UpdateMessageDTO
    {
        public string Content { get; set; } = null!;
    }
}