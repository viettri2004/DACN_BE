using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PaymentService.Application.DTOs;

namespace PaymentService.Application.Interfaces
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPayPaymentRequestModel model);
        VnPayPaymentResponseModel PaymentExecute(IQueryCollection collections);
        Task ProcessVnPayIpnAsync(VnPayPaymentResponseModel response);
    }
}