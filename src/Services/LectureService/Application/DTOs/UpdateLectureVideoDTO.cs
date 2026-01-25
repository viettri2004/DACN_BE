using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LectureService.Application.DTOs
{
    public class UpdateLectureVideoDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        public IFormFile? VideoFile { get; set; }
    }
}