using System;
using System.ComponentModel.DataAnnotations;

namespace LectureService.Application.DTOs
{
    public class CreateLectureDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        [Required]
        public string CourseId { get; set; } = null!;
    }
}