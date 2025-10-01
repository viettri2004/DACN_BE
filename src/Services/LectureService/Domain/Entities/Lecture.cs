using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class Lecture
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public string CourseId { get; set; } = null!;
        public Course Course { get; set; } = null!;
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<LectureVideo> LectureVideos { get; set; } = new List<LectureVideo>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}