using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LectureService.Application.DTOs
{
    public class CreateQuestionOptionDTO
    {
        [Required]
        public string Content { get; set; } = null!;
        public bool IsCorrect { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class CreateQuestionDTO
    {
        [Required]
        public string Content { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public string? Explanation { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePublicId { get; set; }
        public List<CreateQuestionOptionDTO> Options { get; set; } = new List<CreateQuestionOptionDTO>();
    }

    public class CreateQuizDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        [Required]
        public string LectureId { get; set; } = null!;
        public int TestTime { get; set; }
        // public int AttemptCount { get; set; }
        public List<CreateQuestionDTO> Questions { get; set; } = new List<CreateQuestionDTO>();
    }

    public class UpdateQuizDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? TestTime { get; set; }
        // public int? AttemptCount { get; set; }
        public List<CreateQuestionDTO>? Questions { get; set; }
    }
}