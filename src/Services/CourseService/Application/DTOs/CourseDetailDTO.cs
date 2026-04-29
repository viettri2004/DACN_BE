using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseService.Application.DTOs
{
    public class CourseDetailDTO
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string InstructorName { get; set; } = null!;
        public string InstructorJobPosition { get; set; } = null!;
        public int InstructorTotalCourses { get; set; }
        public double Rating { get; set; }
        public int TotalReviews { get; set; }
        public int TotalStudents { get; set; }
        public double TotalHours { get; set; }
        public bool IsEnrolled { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime LastUpdate { get; set; }
        public List<LecturePreviewDTO> Lectures { get; set; } = new List<LecturePreviewDTO>();
    }

    public class LecturePreviewDTO
    {
        // public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public List<VideoPreviewDTO> Videos { get; set; } = new List<VideoPreviewDTO>();
        public List<QuizPreviewDTO> Quizzes { get; set; } = new List<QuizPreviewDTO>();
        public List<DocumentPreviewDTO> Documents { get; set; } = new List<DocumentPreviewDTO>();
    }

    public class VideoPreviewDTO
    {
        // public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public double Duration { get; set; }
        public int DisplayOrder { get; set; }
        public string? VideoUrl { get; set; }
        public bool IsTrial { get; set; }
    }

    public class QuizPreviewDTO
    {
        // public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class DocumentPreviewDTO
    {
        // public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}
