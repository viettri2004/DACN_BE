using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentService.Application.DTOs
{
    public class MomoIpnResponse
    {
        public string partnerCode { get; set; } = null!;
        public string requestId { get; set; } = null!;
        public string orderId { get; set; } = null!;
        public int resultCode { get; set; }
        public string message { get; set; } = null!;
        public long responseTime { get; set; }
        public string signature { get; set; } = null!;
    }
}