using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entities
{
    public class PaymentTransaction
    {
        public string Id { get; set; } = null!;
        public string OrderId { get; set; } = null!;
        public string MoMoTransId { get; set; } = null!;
        public string MoMoRequestId { get; set; } = null!;
        public decimal Amount { get; set; } 
        public string PaymentStatus { get; set; } = null!; 
        public DateTime TransactionDate { get; set; }
        public string GatewayResponse { get; set; } = null!;
        public string ErrorCode { get; set; } = string.Empty;
        public Order Order { get; set; } = null!;
    }
}