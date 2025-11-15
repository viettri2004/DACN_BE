using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentService.Application.DTOs
{
    public class MomoIpnRequest
    {
        public string partnerCode { get; set; } = null!;
        public string orderId { get; set; } = null!;
        public string requestId { get; set; } = null!;
        public long amount { get; set; }
        public string orderInfo { get; set; } = null!;
        public string orderType { get; set; } = null!;
        public long transId { get; set; } 
        public int resultCode { get; set; } 
        public string message { get; set; } = null!;
        public string payType { get; set; } = null!;
        public long responseTime { get; set; }
        public string extraData { get; set; } = null!;
        public string signature { get; set; } = null!;
    }
}