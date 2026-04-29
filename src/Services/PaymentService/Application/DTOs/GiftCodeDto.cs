using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentService.Application.DTOs
{
    public class GiftCodeRedeemDto
    {
        public string Code { get; set; } = null!;
        public string? CourseId { get; set; } // Required if the gift code is for "any course"
    }

    public class CreateGiftCodeDto
    {
        public string Code { get; set; } = null!;
        public string? CourseId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? MaxUses { get; set; }
    }
}
