using System;
using src.Shared.Domain.Entities;

namespace Entities
{
    public class Wishlist
    {
        public string Id { get; set; } = null!;
        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public string CourseId { get; set; } = null!;
        public Course Course { get; set; } = null!;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
