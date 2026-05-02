using System;

namespace CourseService.Application.DTOs
{
    public class UnreadThreadCourseDTO
    {
        public string CourseId { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public string? CourseImage { get; set; }
        public int UnreadThreadCount { get; set; }
        public DateTime LastActivityAt { get; set; }
    }
}
