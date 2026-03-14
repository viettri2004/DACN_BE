using Microsoft.AspNetCore.Identity;

namespace Entities
{
    public class User : IdentityUser
    {
        public string? JobPosition { get; set; }
        public string? Organization { get; set; }
        public string FullName { get; set; } = null!;
        public string? Description { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsBanned { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}