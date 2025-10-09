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
        public double Rating { get; set; }
        public int TotalReviews { get; set; }
        public int TotalStudents { get; set; }
        public double TotalHours { get; set; }
    }
}