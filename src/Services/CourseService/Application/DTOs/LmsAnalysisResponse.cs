using System.Collections.Generic;

namespace CourseService.Application.DTOs
{
    public class LmsAnalysisResponse
    {
        public string Summary { get; set; } = string.Empty;
        public List<VideoSegment> Segments { get; set; } = new();
        public List<SubtitleSegment> Subtitles { get; set; } = new();
    }

    public class VideoSegment
    {
        public string StartTime { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SubtitleSegment
    {
        public double StartTime { get; set; } 
        public double EndTime { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
