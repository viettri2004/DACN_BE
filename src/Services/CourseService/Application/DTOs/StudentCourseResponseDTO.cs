using System.Collections.Generic;

namespace CourseService.Application.DTOs
{
    public class StudentCourseResponseDTO
    {
        public int TotalCourses { get; set; }
        public int CompletedCourses { get; set; }
        public double TotalStudyTime { get; set; }
        public double AverageProgress { get; set; }
        public IEnumerable<CourseListDTO> Courses { get; set; } = new List<CourseListDTO>();
    }
}
