using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentService.Application.DTOs
{
    public class GiftCodeRedeemDto
    {
        public string Code { get; set; } = null!;
        public string? CourseId { get; set; } 
    }

    public class CreateGiftCodeDto
    {
        public string Code { get; set; } = null!;
        public string? CourseId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? MaxUses { get; set; }
    }

    public class UpdateGiftCodeDto
    {
        public string? Code { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? MaxUses { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GiftCodeViewDto
    {
        public string Id { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? CourseId { get; set; }
        public int? MaxUses { get; set; }
        public int UsageCount { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
