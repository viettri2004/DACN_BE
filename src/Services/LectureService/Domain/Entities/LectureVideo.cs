using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class LectureVideo
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string VideoUrl   { get; set; } = null!;
        public string? PublicId { get; set; }
        public double Duration { get; set; }
        public int DisplayOrder { get; set; }
        public string LectureId { get; set; } = null!;
        public Lecture Lecture { get; set; } = null!;
    }
}