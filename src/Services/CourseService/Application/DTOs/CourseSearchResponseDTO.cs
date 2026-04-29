using System.Collections.Generic;
using Shared.Domain.Entities;

namespace CourseService.Application.DTOs
{
    public class CourseSearchResponseDTO
    {
        public PagedResult<CourseCardDTO> Courses { get; set; } = default!;
        public List<TagFacetDTO> AvailableTags { get; set; } = new();
    }
}