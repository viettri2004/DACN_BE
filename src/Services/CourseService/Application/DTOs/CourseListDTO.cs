using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseService.Application.DTOs
{
    public class CourseListDTO
    {
        public string Id { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string InstructorName { get; set; } = null!;
        public double Rating { get; set; }
        public decimal Price { get; set; }
        public int Progress { get; set; }
    }
}