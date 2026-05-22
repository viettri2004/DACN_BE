using System;
using System.Collections.Generic;

namespace OrderingService.Application.DTOs
{
    public class AdminTransactionCourseDto
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Instructor { get; set; } = null!;
        public decimal Price { get; set; }
    }

    public class AdminOrderDto
    {
        public string Id { get; set; } = null!;
        public string GateTransactionId { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public string StudentEmail { get; set; } = null!;
        public string? StudentAvatar { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string CardNum { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = null!;
        public List<AdminTransactionCourseDto> Courses { get; set; } = new();
    }
}
