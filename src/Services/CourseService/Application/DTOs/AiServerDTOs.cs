using System.Collections.Generic;

namespace CourseService.Application.DTOs
{
    public class TranscribeResponse
    {
        public string status { get; set; } = null!;
        public string language { get; set; } = null!;
        public List<AISegment> segments { get; set; } = new();
    }

    public class AISegment
    {
        public double start { get; set; }
        public double end { get; set; }
        public string text { get; set; } = null!;
    }
}
