using System;
using System.Collections.Generic;

namespace PaymentService.Application.DTOs
{
    public class PaymentHistoryDto
    {
        public string Id { get; set; } = null!;
        public List<string> Courses { get; set; } = new();
        public int CourseCount { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTime Date { get; set; }
        public string Status { get; set; } = null!;
        public string Method { get; set; } = null!;
        public string? TransactionId { get; set; }
    }
}
