using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CartService.Application.DTOs
{
    public class AddToCartDTO
    {
        [Required]
        public string CourseId { get; set; } = string.Empty;
    }
}