using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Data.Context;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;

namespace src.Services.PaymentService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IMomoService _momoService;
        private readonly AppDbContext _context;
        private readonly ILogger<PaymentController> _logger;
        private readonly ISepayService _paymentService;
        private readonly BankConfig _bankConfig;
        public PaymentController(IMomoService paymentService, AppDbContext context, ILogger<PaymentController> logger, ISepayService sepayService, IConfiguration configuration)
        {
            _paymentService = sepayService;
            _logger = logger;
            _momoService = paymentService;
            _context = context;
            _bankConfig = configuration.GetSection("Bank").Get<BankConfig>() ?? new BankConfig();
        }

        [Authorize]
        [HttpPost("momo/checkout")]
        public async Task<IActionResult> CreatePayment([FromBody] CheckoutRequestDto checkoutRequest)
        {
            var studentId = User.Claims.FirstOrDefault(c =>
                c.Type == "id")?.Value;

            if (studentId == null)
            {
                return Unauthorized();
            }

            var courses = await _context.Courses
                .Where(c => checkoutRequest.CourseIds.Contains(c.Id))
                .ToListAsync();

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

            foreach (var course in courses)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    CourseId = course.Id,
                    Price = course.Price,
                    FinalPrice = course.Price 
                });
            }

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            try
            {
                var momoResponse = await _momoService.CreatePaymentRequestAsync(order);

                if (momoResponse.resultCode == 0)
                {
                    return Ok(new { payUrl = momoResponse.payUrl });
                }
                return BadRequest(momoResponse.message);
            }
            catch (Exception ex)

            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("momo/ipn")]
        public async Task<IActionResult> MoMoIpnHandler([FromBody] MomoIpnRequest request)
        {
            _logger.LogInformation("Nhận được IPN từ MoMo, OrderId: {OrderId}, ResultCode: {ResultCode}",
                request.orderId,
                request.resultCode);

            _logger.LogDebug(JsonSerializer.Serialize(request));

            try
            {
                await _momoService.ProcessMoMoIpnAsync(request);

                var response = new MomoIpnResponse
                {
                    partnerCode = "MOMO",
                    requestId = request.requestId,
                    orderId = request.orderId,
                    resultCode = 0,
                    message = "Success",
                    responseTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý IPN cho OrderId: {OrderId}", request.orderId);
                return StatusCode(500, new { resultCode = 99, message = ex.Message });
            }
        }
        [HttpPost("sepay/checkout-bank")]
        [Authorize] 
        public async Task<IActionResult> CreateBankCheckout([FromBody] CheckoutRequestDto checkoutRequest)
        {
            var studentId = User.Claims.FirstOrDefault(c =>
                c.Type == "id")?.Value;

            if (studentId == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(studentId)) return Unauthorized();

            var courses = await _context.Courses
                .Where(c => checkoutRequest.CourseIds.Contains(c.Id))
                .ToListAsync();

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

            foreach (var course in courses)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    CourseId = course.Id,
                    Price = course.Price,
                    FinalPrice = course.Price
                });
            }

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            string vietQrPayload = VietQrHelper.GenerateVietQrPayload(
                _bankConfig.AccountNumber,
                _bankConfig.AccountName,
                (long)order.TotalAmount,
                order.Id
            );

            string qrCodeBase64 = QrCodeGenerator.GenerateQrCodeBase64(vietQrPayload);

            return Ok(new
            {
                Message = "Vui lòng chuyển khoản để hoàn tất đơn hàng.",
                OrderId = order.Id,
                TotalAmount = order.TotalAmount,
                BankName = _bankConfig.BankName,
                AccountNumber = _bankConfig.AccountNumber,
                AccountName = _bankConfig.AccountName,
                PaymentContent = order.Id,
                QrCodeBase64 = qrCodeBase64
            });
        }

        [HttpPost("sepay/webhook")]
        public async Task<IActionResult> SepayWebhookHandler([FromBody] SepayWebhookRequest request)
        {
            try
            {
                _logger.LogInformation("Nhận được Webhook từ Sepay, ID: {Id}, Code: {Code}, Amount: {Amount}",
                    request.Id, request.Code, request.TransferAmount);

                await _paymentService.ProcessSepayWebhookAsync(request);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý Sepay Webhook cho Code: {Code}", request.Code);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

    }
    public class CheckoutRequestDto
    {
        public List<string> CourseIds { get; set; } = new List<string>();
    }
}