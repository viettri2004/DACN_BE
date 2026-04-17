using System;

namespace Entities
{
    public class StudentLectureProgress
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StudentId { get; set; } = null!;
        public string LectureId { get; set; } = null!;
        public string CourseId { get; set; } = null!;
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
        
        public Course Course { get; set; } = null!;
    }
}
