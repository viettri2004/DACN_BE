using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LectureService.Application.DTOs
{
    public class LectureVideoDTO
    {
        public string Name { get; set; } = null!;
        public string VideoUrl { get; set; } = null!;
        public double Duration { get; set; }
        public object? AnalysisResult { get; set; }
    }
}