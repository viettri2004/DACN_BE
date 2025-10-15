using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class OrderItem
    {
        public string Id { get; set; } = null!;
        public string OrderId { get; set; } = null!;
        public Order Order { get; set; } = null!;

        public string CourseId { get; set; } = null!;
        public Course Course { get; set; } = null!;

        public decimal Price { get; set; }
        public decimal FinalPrice { get; set; }
    }
}