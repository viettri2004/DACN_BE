using System;
using System.Collections.Generic;

namespace Entities
{
    public class QAThread
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        public string CourseId { get; set; } = null!;
        public Course Course { get; set; } = null!;

        public string CreatorId { get; set; } = null!;
        public User Creator { get; set; } = null!;

        public ICollection<QAMessage> Messages { get; set; } = new List<QAMessage>();
    }
}