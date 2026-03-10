using System;

namespace CourseService.Application.DTOs
{
    public class InstructorCourseListDTO
    {
        public string Id { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int TotalStudents { get; set; }
        public double Rating { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
    }
}