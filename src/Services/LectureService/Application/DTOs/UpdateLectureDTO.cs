using System;
using System.ComponentModel.DataAnnotations;

namespace LectureService.Application.DTOs
{
    public class UpdateLectureDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
    }
}