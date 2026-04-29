using System;

namespace CourseService.Application.DTOs
{
    public class TagFacetDTO
    {
        public string TagId { get; set; } = string.Empty;
        public string TagName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}