using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace CourseService.Application.DTOs
{
    public class UpdateCourseDTO
    {
        [Required]
        public string name { get; set; } = null!;
        
        [Required]
        public decimal price { get; set; }

        public string? description { get; set; }

        public string? imageUrl { get; set; }
        public string? imagePublicId { get; set; }
        public List<string>? TagIds { get; set; }
    }
}
