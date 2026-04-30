using Microsoft.AspNetCore.Http;
using System;

namespace AccountService.Application.DTOs
{
    public class UpdateUserProfileDTO
    {
        public string? FullName { get; set; }
        public string? JobPosition { get; set; }
        public string? Organization { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Description { get; set; }
        public string? AvatarUrl { get; set; }
        public string? AvatarPublicId { get; set; }
    }
}
