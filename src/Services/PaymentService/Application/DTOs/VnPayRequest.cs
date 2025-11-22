using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentService.Application.DTOs
{
    public class VnPayPaymentRequestModel
    {
        public string OrderId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
    }
}