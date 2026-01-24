using System.Collections.Generic;

namespace CourseService.Application.DTOs
{
    public class CourseContentDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public IEnumerable<LectureContentDTO> Lectures { get; set; } = new List<LectureContentDTO>();
    }

    public class LectureContentDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public IEnumerable<VideoContentDTO> Videos { get; set; } = new List<VideoContentDTO>();
        public IEnumerable<string> DocumentNames { get; set; } = new List<string>();
        public IEnumerable<string> QuizNames { get; set; } = new List<string>();
    }
    public class VideoContentDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}
