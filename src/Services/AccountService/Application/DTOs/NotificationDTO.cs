using System;

namespace AccountService.Application.DTOs
{
    public class NotificationDTO
    {
        public string Id { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
