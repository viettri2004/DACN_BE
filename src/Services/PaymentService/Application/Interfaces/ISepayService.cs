using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PaymentService.Application.DTOs;

namespace PaymentService.Application.Interfaces
{
    public interface ISepayService
    {
        Task ProcessSepayWebhookAsync(SepayWebhookRequest request);
    }
}