using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class LectureVideo
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? ReplyId { get; set; } 
        public Comment? Parent { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
        public string LectureId { get; set; } = null!;
        public Lecture Lecture { get; set; } = null!;
    }
}