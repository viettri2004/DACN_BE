using System;
using System.ComponentModel.DataAnnotations;

namespace src.Shared.Domain.Entities
{
    public class Notification
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? UserId { get; set; } // Null if sent to all admins
        public string? Role { get; set; } // "Admin", "Student", "Instructor"
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!; // InstructorRequest, CourseRequest, etc.
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
