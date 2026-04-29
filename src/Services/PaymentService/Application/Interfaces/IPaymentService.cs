using System.Threading.Tasks;
using PaymentService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace PaymentService.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse> CreateBankPaymentAsync(CheckoutRequestDto checkoutRequest, string studentId);
        Task<ApiResponse> CreateVnPayPaymentAsync(CheckoutRequestDto checkoutRequest, string studentId);
        Task<ApiResponse> RedeemGiftCodeAsync(GiftCodeRedeemDto redeemDto, string studentId);
        Task<ApiResponse> CreateGiftCodeAsync(CreateGiftCodeDto createDto, string userId);
        Task<ApiResponse> GetPaymentHistoryAsync(string studentId);
    }
}