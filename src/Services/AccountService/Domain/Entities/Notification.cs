using System;
using System.ComponentModel.DataAnnotations;
using AccountService.Domain.Enums;

namespace Entities
{
    public class Notification
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? UserId { get; set; } // Null if sent to all admins/roles
        public NotificationRole? Role { get; set; } // Admin, Student, Instructor
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
