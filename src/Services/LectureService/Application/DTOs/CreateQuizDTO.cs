using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LectureService.Application.DTOs
{
    public class CreateQuestionDTO
    {
        [Required]
        public string Question { get; set; } = null!;
        [Required]
        public string Key { get; set; } = null!; 
        public string? Description { get; set; } 
    }

    public class CreateQuizDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string LectureId { get; set; } = null!;
        public int TestTime { get; set; }
        public int AttemptCount { get; set; }
        public List<CreateQuestionDTO> Questions { get; set; } = new List<CreateQuestionDTO>();
    }

    public class UpdateQuizQuestionsDTO
    {
        [Required]
        public string QuizId { get; set; } = null!;
        public List<CreateQuestionDTO> Questions { get; set; } = new List<CreateQuestionDTO>();
    }
}