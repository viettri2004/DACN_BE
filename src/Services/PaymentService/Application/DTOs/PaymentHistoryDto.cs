using System;

namespace PaymentService.Application.DTOs
{
    public class PaymentHistoryDto
    {
        public string Id { get; set; } = null!;
        public string Course { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTime Date { get; set; }
        public string Status { get; set; } = null!;
        public string Method { get; set; } = null!;
        public string? TransactionId { get; set; }
    }
}
