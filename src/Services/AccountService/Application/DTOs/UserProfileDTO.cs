using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Application.DTOs
{
    public class UserProfileDTO
    {
        public string Username { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string JobPosition { get; set; } = null!;
        public string Organization { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string AvatarUrl { get; set; } = null!;
        public int? MemberSinceYear { get; set; }
        public UserLearningStatsDTO Stats { get; set; } = null!;
    }
}