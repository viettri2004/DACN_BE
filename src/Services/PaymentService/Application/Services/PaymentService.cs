using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities;
using Microsoft.Extensions.Configuration;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;
using src.Shared.Domain.Entities;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Resources;

namespace PaymentService.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IVnPayService _vnPayService;
        private readonly BankConfig _bankConfig;
        private readonly IDistributedCache _cache;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IVnPayService vnPayService,
            IConfiguration configuration,
            IDistributedCache cache,
            IStringLocalizer<SharedResources> localizer)
        {
            _paymentRepository = paymentRepository;
            _vnPayService = vnPayService;
            _bankConfig = configuration.GetSection("Bank").Get<BankConfig>() ?? new BankConfig();
            _cache = cache;
            _localizer = localizer;
        }

        public async Task<ApiResponse> CreateBankPaymentAsync(CheckoutRequestDto checkoutRequest, string studentId)
        {
            try
            {
                var courses = await _paymentRepository.GetCoursesByIdsAsync(checkoutRequest.CourseIds);

                var existingEnrollments = await _paymentRepository.GetEnrollmentsByStudentAndCoursesAsync(studentId, checkoutRequest.CourseIds);
                if (existingEnrollments.Any())
                {
                    var ownedCourseIds = existingEnrollments.Select(e => e.CourseId).ToList();
                    return new ApiResponse("Conflict", _localizer["OwnedCoursesConflict"].Value, new { ownedCourses = ownedCourseIds }, false);
                }

                var totalAmount = courses.Sum(c => c.Price);

                var order = new Order
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = studentId,
                    TotalAmount = totalAmount,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending",
                    PaymentMethod = "Sepay_MBBank",
                    MoMoRequestId = null!
                };

                var orderItems = courses.Select(course => new OrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    CourseId = course.Id,
                    Price = course.Price,
                    FinalPrice = course.Price
                }).ToList();

                await _paymentRepository.CreateOrderAsync(order);
                await _paymentRepository.AddOrderItemsAsync(orderItems);
                await _paymentRepository.SaveChangesAsync();

                string vietQrPayload = VietQrHelper.GenerateVietQrPayload(
                    _bankConfig.AccountNumber,
                    _bankConfig.AccountName,
                    (long)order.TotalAmount,
                    order.Id
                );

                string qrCodeBase64 = QrCodeGenerator.GenerateQrCodeBase64(vietQrPayload);

                var responseData = new
                {
                    Message = "Vui lòng chuyển khoản để hoàn tất đơn hàng.",
                    OrderId = order.Id,
                    TotalAmount = order.TotalAmount,
                    BankName = _bankConfig.BankName,
                    AccountNumber = _bankConfig.AccountNumber,
                    AccountName = _bankConfig.AccountName,
                    PaymentContent = order.Id,
                    QrCodeBase64 = qrCodeBase64
                };

                return new ApiResponse("Success", _localizer["BankPaymentCreated"].Value, responseData, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> CreateVnPayPaymentAsync(CheckoutRequestDto checkoutRequest, string studentId)
        {
            try
            {
                var courses = await _paymentRepository.GetCoursesByIdsAsync(checkoutRequest.CourseIds);

                var existingEnrollments = await _paymentRepository.GetEnrollmentsByStudentAndCoursesAsync(studentId, checkoutRequest.CourseIds);
                if (existingEnrollments.Any())
                {
                    var ownedCourseIds = existingEnrollments.Select(e => e.CourseId).ToList();
                    return new ApiResponse("Conflict", _localizer["OwnedCoursesConflict"].Value, new { ownedCourses = ownedCourseIds }, false);
                }

                var totalAmount = courses.Sum(c => c.Price);

                var order = new Order
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = studentId,
                    TotalAmount = totalAmount,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending",
                    PaymentMethod = "VnPay",
                    MoMoRequestId = null!
                };

                var orderItems = courses.Select(course => new OrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    CourseId = course.Id,
                    Price = course.Price,
                    FinalPrice = course.Price
                }).ToList();

                await _paymentRepository.CreateOrderAsync(order);
                await _paymentRepository.AddOrderItemsAsync(orderItems);
                await _paymentRepository.SaveChangesAsync();

                var vnPayRequest = new VnPayPaymentRequestModel
                {
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    Description = $"Thanh toán cho đơn hàng {order.Id}",
                    CreatedDate = order.CreatedAt
                };

                var payUrl = _vnPayService.CreatePaymentUrl(null, vnPayRequest);

                return new ApiResponse("Success", _localizer["VnPayPaymentCreated"].Value, new { payUrl }, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> CreateGiftCodeAsync(CreateGiftCodeDto createDto)
        {
            try
            {
                var existingCode = await _paymentRepository.GetGiftCodeByCodeAsync(createDto.Code);
                if (existingCode != null)
                {
                    return new ApiResponse("Conflict", _localizer["GiftCodeExists"].Value, null, false);
                }

                var giftCode = new GiftCode
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = createDto.Code,
                    CourseId = createDto.CourseId,
                    ExpiryDate = createDto.ExpiryDate,
                    IsActive = true,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _paymentRepository.AddGiftCodeAsync(giftCode);
                await _paymentRepository.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["GiftCodeCreated"].Value, giftCode, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> RedeemGiftCodeAsync(GiftCodeRedeemDto redeemDto, string studentId)
        {
            try
            {
                var giftCode = await _paymentRepository.GetGiftCodeByCodeAsync(redeemDto.Code);

                if (giftCode == null || !giftCode.IsActive)
                {
                    return new ApiResponse("NotFound", _localizer["InvalidGiftCode"].Value, null, false);
                }

                if (giftCode.IsUsed)
                {
                    return new ApiResponse("BadRequest", _localizer["GiftCodeUsed"].Value, null, false);
                }

                if (giftCode.ExpiryDate.HasValue && giftCode.ExpiryDate.Value < DateTime.UtcNow)
                {
                    return new ApiResponse("BadRequest", _localizer["GiftCodeExpired"].Value, null, false);
                }

                string? courseIdToRedeem = giftCode.CourseId ?? redeemDto.CourseId;

                if (string.IsNullOrEmpty(courseIdToRedeem))
                {
                    return new ApiResponse("BadRequest", _localizer["SelectCourseForGiftCode"].Value, null, false);
                }

                var course = await _paymentRepository.GetCourseByIdAsync(courseIdToRedeem);
                if (course == null)
                {
                    return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);
                }

                var existingEnrollment = await _paymentRepository.GetEnrollmentsByStudentAndCoursesAsync(studentId, new List<string> { courseIdToRedeem });
                if (existingEnrollment.Any())
                {
                    return new ApiResponse("Conflict", _localizer["OwnedCourseConflict"].Value, null, false);
                }

                // Mark as used
                giftCode.IsUsed = true;
                giftCode.UsedAt = DateTime.UtcNow;
                giftCode.UsedByStudentId = studentId;

                // Create a free Order for the gift code redemption
                var order = new Order
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = studentId,
                    TotalAmount = 0,
                    CreatedAt = DateTime.UtcNow,
                    PaidAt = DateTime.UtcNow,
                    Status = "Paid",
                    PaymentMethod = $"GiftCode: {giftCode.Code}",
                    MoMoRequestId = ""
                };

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    CourseId = courseIdToRedeem,
                    Price = course.Price,
                    FinalPrice = 0 // Free
                };

                await _paymentRepository.CreateOrderAsync(order);
                await _paymentRepository.AddOrderItemsAsync(new List<OrderItem> { orderItem });

                // Create enrollment linked to the new order
                var enrollment = new Enrollment
                {
                    Id = Guid.NewGuid().ToString(),
                    CourseId = courseIdToRedeem,
                    StudentId = studentId,
                    OrderId = order.Id,
                    EnrolledAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(100),
                    Status = true
                };

                await _paymentRepository.AddEnrollmentAsync(enrollment);

                // Clear cart from database
                await _paymentRepository.RemoveCartItemsAsync(studentId, new List<string> { courseIdToRedeem });
                
                await _paymentRepository.SaveChangesAsync();

                // Clear cart from Redis cache
                await _cache.RemoveAsync($"cart:{studentId}");

                return new ApiResponse("Success", _localizer["GiftCodeRedeemedSuccess", course.Name].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
    }
}