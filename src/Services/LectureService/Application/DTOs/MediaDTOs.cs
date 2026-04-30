using System.ComponentModel.DataAnnotations;

namespace LectureService.Application.DTOs
{
    public class UpdateMediaDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        public string? Url { get; set; }
        public string? PublicId { get; set; }
        public double? Duration { get; set; } // For video
        public string? Type { get; set; }     // For document
    }

    public class AddMediaDTO
    {
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string Url { get; set; } = null!;
        [Required]
        public string PublicId { get; set; } = null!;
        public double Duration { get; set; } // For video
        public string? Type { get; set; }     // For document
    }
}