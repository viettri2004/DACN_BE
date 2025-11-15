using System.Threading.Tasks;
using PaymentService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace PaymentService.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse> CreateMoMoPaymentAsync(CheckoutRequestDto checkoutRequest, string studentId);
        Task<ApiResponse> CreateBankPaymentAsync(CheckoutRequestDto checkoutRequest, string studentId);
    }
}