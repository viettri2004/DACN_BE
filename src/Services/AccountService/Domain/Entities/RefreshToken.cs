using System;
using Entities;

namespace Entities
{
    public class RefreshToken
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Token { get; set; } = null!;
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;
        public DateTime ExpiryDate { get; set; }
        public bool IsRevoked { get; set; }
        public string UserId { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
