using System.Collections.Generic;

namespace LectureService.Application.DTOs
{
    public class QuestionOptionDTO
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsCorrect { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class QuestionDTO
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public string? Explanation { get; set; }
        public List<QuestionOptionDTO> Options { get; set; } = new List<QuestionOptionDTO>();
    }

    public class QuizDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string LectureId { get; set; } = null!;
        public int TestTime { get; set; }
        // public int AttemptCount { get; set; }
        public List<QuestionDTO> Questions { get; set; } = new List<QuestionDTO>();
    }
}
