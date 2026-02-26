using System.Collections.Generic;

namespace LectureService.Application.DTOs
{
    public class QuizAnswerDTO
    {
        public string QuestionId { get; set; } = null!;
        public string SelectedOptionId { get; set; } = null!;
    }

    public class QuizSubmissionDTO
    {
        public string QuizAttemptId { get; set; } = null!;
        public List<QuizAnswerDTO> Answers { get; set; } = new List<QuizAnswerDTO>();
    }
}
