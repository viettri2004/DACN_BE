using System;

namespace CourseService.Application.DTOs
{
    public class AdminCourseListDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string InstructorName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public decimal Price { get; set; }
        public DateTime CreateTime { get; set; }
        public int TotalStudents { get; set; }
    }
}
