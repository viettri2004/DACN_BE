using System;
using System.ComponentModel.DataAnnotations;
using Entities;

namespace CourseService.Domain.Entities
{
    public class CourseFaq
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string Question { get; set; } = null!;
        
        [Required]
        public string Answer { get; set; } = null!;
        
        public int DisplayOrder { get; set; }
        
        public string CourseId { get; set; } = null!;
        public Course Course { get; set; } = null!;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}