using System;

namespace CourseService.Application.DTOs
{
    public class CoursePreviewDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string InstructorName { get; set; } = null!;
    }
}
