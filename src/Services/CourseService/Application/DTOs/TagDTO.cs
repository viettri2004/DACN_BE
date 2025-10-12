using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src.Services.CourseService.Application.DTOs
{
    public class TagDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}