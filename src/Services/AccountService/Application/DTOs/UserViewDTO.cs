using System;

namespace AccountService.Application.DTOs
{
    public class UserViewDTO
    {
        public string Id { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = null!;
        public bool IsBanned { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
