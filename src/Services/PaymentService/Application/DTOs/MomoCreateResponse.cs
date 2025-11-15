using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentService.Application.DTOs
{
    public class MomoCreateResponse
    {
        public string partnerCode { get; set; } = null!;
        public string requestId { get; set; } = null!;
        public string orderId { get; set; } = null!;
        public long amount { get; set; }
        public long responseTime { get; set; }
        public string message { get; set; } = null!;
        public int resultCode { get; set; } 
        public string payUrl { get; set; } = null!; 
        public string deeplink { get; set; } = null!; 
        public string qrCodeUrl { get; set; } = null!; 
        public string deeplinkMiniApp { get; set; } = null!;
        public string signature { get; set; } = null!;
    }
}