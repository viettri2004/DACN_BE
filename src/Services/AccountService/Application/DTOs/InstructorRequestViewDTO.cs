using System;

namespace AccountService.Application.DTOs
{
    public class InstructorRequestViewDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        
        public string? Experience { get; set; }
        public string? Expertise { get; set; }
        public string? Certificate { get; set; }
        public string? Introduction { get; set; }
        public string? SocialLinks { get; set; }
        
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}