using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentService.Application.DTOs
{
    public class MomoCreateRequest
    {
        public string partnerCode { get; set; } = null!;
        public string requestId { get; set; } = null!;
        public long amount { get; set; }
        public string orderId { get; set; } = null!;
        public string orderInfo { get; set; } = null!;
        public string redirectUrl { get; set; } = null!;
        public string ipnUrl { get; set; } = null!;
        public string requestType { get; set; } = "captureWallet";
        public string extraData { get; set; } = "";
        public string lang { get; set; } = "vi";
        public string signature { get; set; } = null!;
    }
}