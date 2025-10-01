using Microsoft.AspNetCore.Identity;

namespace Entities
{
    public class User : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
    }
}