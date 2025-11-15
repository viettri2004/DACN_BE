using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities;
using PaymentService.Application.DTOs;

namespace PaymentService.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<MomoCreateResponse> CreatePaymentRequestAsync(Order order);
        Task ProcessMoMoIpnAsync(MomoIpnRequest request);
    }
}