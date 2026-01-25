using System.ComponentModel.DataAnnotations;

namespace LectureService.Application.DTOs
{
    public class UpdateOrderDTO
    {
        [Required]
        public string Id { get; set; } = null!;
        [Required]
        public int DisplayOrder { get; set; }
    }
}