using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class Order
    {
        public string Id { get; set; } = null!;
        public string StudentId { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Pending"; 
        public string PaymentMethod { get; set; } = "MoMo"; 
        public string MoMoRequestId { get; set; } = string.Empty; 
        public DateTime? PaidAt { get; set; }
        public Student Student { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    }
}