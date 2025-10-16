using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CartService.Application.DTOs
{
    public class CartDTO
    {
        public List<CartItemDTO> Items { get; set; } = new List<CartItemDTO>();
        public decimal TotalPrice { get; set; }
        public int TotalItems { get; set; }
    }
}