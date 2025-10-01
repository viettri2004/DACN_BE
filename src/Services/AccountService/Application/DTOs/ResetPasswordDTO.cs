using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Application.DTOs
{
    public class ResetPasswordDTO
    {
        [Required]
        public string Email { get; set; } = string.Empty;
                public string Otp { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}