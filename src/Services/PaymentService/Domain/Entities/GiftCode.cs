using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class GiftCode
    {
        public string Id { get; set; } = null!;
        public string Code { get; set; } = null!; // Unique code
        public string? CourseId { get; set; } // If null, applicable for any course
        public Course? Course { get; set; }
        
        public int? MaxUses { get; set; } // Null for infinite
        public int UsageCount { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
