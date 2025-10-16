using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseService.Application.DTOs
{
    public class CourseCardDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string InstructorName { get; set; } = null!;
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int TotalStudents { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int TotalHours { get; set; }
        public bool IsBestseller { get; set; }
    }
}