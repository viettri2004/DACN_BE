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
        private readonly IVnPayService _vnPayService;
        private readonly BankConfig _bankConfig;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IVnPayService vnPayService,
            IConfiguration configuration)
        {
            _paymentRepository = paymentRepository;
            _vnPayService = vnPayService;
            _bankConfig = configuration.GetSection("Bank").Get<BankConfig>() ?? new BankConfig();
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
                    return new ApiResponse("Conflict", "Bạn đã sở hữu một số khóa học trong đơn hàng này.", new { ownedCourses = ownedCourseIds }, false);
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

                return new ApiResponse("Success", "Bank payment created successfully", responseData, true);
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
                    return new ApiResponse("Conflict", "Bạn đã sở hữu một số khóa học trong đơn hàng này.", new { ownedCourses = ownedCourseIds }, false);
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

                return new ApiResponse("Success", "VnPay payment created successfully", new { payUrl }, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
    }
}