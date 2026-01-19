using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities
{
    public class InstructorRequest
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        
        public string? Experience { get; set; }
        public string? Expertise { get; set; }
        public string? Certificate { get; set; }
        public string? Introduction { get; set; }
        public string? SocialLinks { get; set; }
        
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string? AdminId { get; set; }
        [ForeignKey("AdminId")]
        public User? Admin { get; set; } 
        
        public DateTime? ProcessedAt { get; set; }
    }
}