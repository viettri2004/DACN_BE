using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class CartItem
    {
        public string Id { get; set; } = null!;

        public string CartId { get; set; } = null!;
        public Cart Cart { get; set; } = null!;

        public string CourseId { get; set; } = null!;
        public Course Course { get; set; } = null!;

        public decimal Price { get; set; }
    }
}