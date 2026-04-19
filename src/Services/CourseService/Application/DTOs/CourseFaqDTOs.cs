using System.ComponentModel.DataAnnotations;

namespace CourseService.Application.DTOs
{
    public class CreateCourseFaqDTO
    {
        [Required]
        public string Question { get; set; } = null!;
        
        [Required]
        public string Answer { get; set; } = null!;
        
        public int DisplayOrder { get; set; }
    }
    
    public class UpdateCourseFaqDTO
    {
        [Required]
        public string Question { get; set; } = null!;
        
        [Required]
        public string Answer { get; set; } = null!;
        
        public int DisplayOrder { get; set; }
    }
}