using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LectureService.Application.DTOs
{
    public class UpdateLectureFileDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        public IFormFile? File { get; set; }
    }
}