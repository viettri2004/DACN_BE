using System.Collections.Generic;
using Entities;

namespace CourseService.Application.DTOs
{
    public class CourseContentDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public IEnumerable<string> Tags { get; set; } = new List<string>();
        public int Progress { get; set; }
        public string InstructorName { get; set; } = null!;
        public string InstructorJobPosition { get; set; } = null!;
        public int InstructorTotalCourses { get; set; }
        public double Rating { get; set; }
        public int TotalReviews { get; set; }
        public int TotalStudents { get; set; }
        public double TotalHours { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public double TotalStudyTime { get; set; }
        public IEnumerable<LectureContentDTO> Lectures { get; set; } = new List<LectureContentDTO>();
    }

    public class LectureContentDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsCompleted { get; set; }
        public IEnumerable<VideoContentDTO> Videos { get; set; } = new List<VideoContentDTO>();
        public IEnumerable<DocumentContentDTO> Documents { get; set; } = new List<DocumentContentDTO>();
        public IEnumerable<QuizContentDTO> Quizzes { get; set; } = new List<QuizContentDTO>();
    }
    public class VideoContentDTO
    {
        public string Id { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public string Name { get; set; } = null!;
        public double Duration { get; set; }
        public bool IsCompleted { get; set; }
    }
    public class DocumentContentDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsCompleted { get; set; }
    }
    public class QuizContentDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsCompleted { get; set; }
    }
}
