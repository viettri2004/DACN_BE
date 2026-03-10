using System;
using CourseService.Domain.Enums;

namespace CourseService.Application.DTOs
{
    public class CourseRequestDTO
    {
        public string Id { get; set; } = null!;
        public string CourseId { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public string InstructorId { get; set; } = null!;
        public string InstructorName { get; set; } = null!;
        public decimal CoursePrice { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}