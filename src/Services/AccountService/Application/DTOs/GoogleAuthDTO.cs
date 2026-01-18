using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Application.DTOs
{
    public class GoogleAuthDTO
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
        
        public string? Role { get; set; } = "Student";
    }
}
