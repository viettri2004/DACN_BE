using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class Lecture
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<LectureVideo> LectureVideos { get; set; } = new List<LectureVideo>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}