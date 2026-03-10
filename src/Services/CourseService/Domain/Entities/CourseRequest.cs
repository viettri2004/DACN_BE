using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CourseService.Domain.Enums;
using Entities;

namespace CourseService.Domain.Entities
{
    public class CourseRequest
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CourseId { get; set; } = null!;

        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        [Required]
        public string InstructorId { get; set; } = null!;

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ProcessedAt { get; set; }

        // public string? Reason { get; set; }
    }
}