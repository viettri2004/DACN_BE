using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities;
using Microsoft.Extensions.Configuration;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;
using src.Shared.Domain.Entities;

namespace PaymentService.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IMomoService _momoService;
        private readonly BankConfig _bankConfig;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IMomoService momoService,
            IConfiguration configuration)
        {
            _paymentRepository = paymentRepository;
            _momoService = momoService;
            _bankConfig = configuration.GetSection("Bank").Get<BankConfig>() ?? new BankConfig();
        }

        public async Task<ApiResponse> CreateMoMoPaymentAsync(CheckoutRequestDto checkoutRequest, string studentId)
        {
            try
            {
                var courses = await _paymentRepository.GetCoursesByIdsAsync(checkoutRequest.CourseIds);
                var totalAmount = courses.Sum(c => c.Price);

                var order = new Order
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = studentId,
                    TotalAmount = totalAmount,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending",
                    MoMoRequestId = Guid.NewGuid().ToString()
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

                var momoResponse = await _momoService.CreatePaymentRequestAsync(order);

                if (momoResponse.resultCode == 0)
                {
                    return new ApiResponse("Success", "Payment created successfully", new { payUrl = momoResponse.payUrl }, true);
                }

                return new ApiResponse("BadRequest", momoResponse.message, null, false);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> CreateBankPaymentAsync(CheckoutRequestDto checkoutRequest, string studentId)
        {
            try
            {
                var courses = await _paymentRepository.GetCoursesByIdsAsync(checkoutRequest.CourseIds);
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

                return new ApiResponse("Success", "Bank payment created successfully", responseData, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
    }
}