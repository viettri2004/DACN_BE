using System;
using System.Collections.Generic;

namespace CourseService.Application.DTOs
{
    public class QuestionAnswerDTO
    {
        public string Id { get; set; } = null!;
        public string? Title { get; set; }
        public string Content { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsMyQA { get; set; }
        public List<QuestionAnswerDTO> Replies { get; set; } = new List<QuestionAnswerDTO>();
    }

    public class CreateQuestionDTO
    {
        public string CourseId { get; set; } = null!;
        public string? Title { get; set; }
        public string Content { get; set; } = null!;
    }

    public class ReplyQADTO
    {
        public string ParentId { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    public class UpdateQADTO
    {
        public string? Title { get; set; }
        public string Content { get; set; } = null!;
    }
}
