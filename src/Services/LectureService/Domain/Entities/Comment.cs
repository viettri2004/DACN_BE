using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class Comment
    {
        public int Id { get; set; }

        public string Content { get; set; } = null!;

        public int? ReplyId { get; set; }
        public Comment? Parent { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

        public int LectureId { get; set; }
        public Lecture Lecture { get; set; } = null!;
    }
}