using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseService.Application.DTOs
{
    public class AssignTagToCourseDTO
    {
        public required List<string> TagId { get; set; }
        public required string CourseId { get; set; }
    }
}