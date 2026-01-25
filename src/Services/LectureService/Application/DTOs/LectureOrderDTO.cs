using System.ComponentModel.DataAnnotations;

namespace LectureService.Application.DTOs
{
    public class LectureOrderDTO
    {
        [Required]
        public string LectureId { get; set; } = null!;
        [Required]
        public int DisplayOrder { get; set; }
    }
}