using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.Interfaces;
using OrderingService.Domain.Entities;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using LearningService.Application.Services;
using LearningService.Application.Interfaces;
using LearningService.Domain.Entities;
using InteractionService.Application.DTOs;
using InteractionService.Application.Interfaces;
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using System.Threading.Tasks;
using OrderingService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace OrderingService.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse> CreateBankPaymentAsync(CheckoutRequestDto checkoutRequest, string studentId);
        Task<ApiResponse> CreateVnPayPaymentAsync(CheckoutRequestDto checkoutRequest, string studentId);
        Task<ApiResponse> RedeemGiftCodeAsync(GiftCodeRedeemDto redeemDto, string studentId);
        Task<ApiResponse> CreateGiftCodeAsync(CreateGiftCodeDto createDto, string userId);
        Task<ApiResponse> UpdateGiftCodeAsync(string giftCodeId, UpdateGiftCodeDto updateDto, string userId);
        Task<ApiResponse> DeleteGiftCodeAsync(string giftCodeId, string userId);
        Task<ApiResponse> GetGiftCodesByCourseAsync(string courseId, string userId);
        Task<ApiResponse> GetPaymentHistoryAsync(string studentId, int pageNumber, int pageSize);
        Task<ApiResponse> GetAdminTransactionsAsync(string status, string paymentMethod, string searchTerm, int pageNumber, int pageSize);
    }
}


