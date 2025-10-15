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
        public Student Student { get; set; } = null!;

        public decimal TotalAmount { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}