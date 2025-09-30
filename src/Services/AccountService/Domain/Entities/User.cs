using Microsoft.AspNetCore.Identity;

namespace Entities
{
    public class User : IdentityUser<int>
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
    }
}