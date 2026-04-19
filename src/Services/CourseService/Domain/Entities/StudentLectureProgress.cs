using System;

namespace Entities
{
    public class StudentLectureProgress
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StudentId { get; set; } = null!;
        public string LectureId { get; set; } = null!;
        public string CourseId { get; set; } = null!;
        public string ItemId { get; set; } = null!;
        public string ItemType { get; set; } = null!; // "Video", "Document", "Quiz"
        public bool IsCompleted { get; set; } = false;
        
        public Course Course { get; set; } = null!;
    }
}
