using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Data.Context;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;

namespace src.Services.PaymentService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly AppDbContext _context;
        private readonly ILogger<PaymentController> _logger;
        public PaymentController(IPaymentService paymentService, AppDbContext context, ILogger<PaymentController> logger)
        {
            _logger = logger;
            _paymentService = paymentService;
            _context = context;
        }

        /// <summary>   
        /// API 1: Client gọi để bắt đầu thanh toán (Tạo Order và lấy link MoMo)
        /// </summary>
        [Authorize]
        [HttpPost("checkout")]
        public async Task<IActionResult> CreatePayment([FromBody] CheckoutRequestDto checkoutRequest)
        {
            // Giả định: Client gửi lên StudentId và danh sách CourseId
            // 1. Tạo Order (Logic này nên nằm trong OrderService, nhưng để đơn giản tôi làm ở đây)
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
                MoMoRequestId = Guid.NewGuid().ToString() // Mã requestId duy nhất
            };

            foreach (var course in courses)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    CourseId = course.Id,
                    Price = course.Price,
                    FinalPrice = course.Price // (Có thể thêm logic giảm giá ở đây)
                });
            }

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // 2. Gọi MoMo Service để lấy URL thanh toán
            try
            {
                var momoResponse = await _paymentService.CreatePaymentRequestAsync(order);

                if (momoResponse.resultCode == 0)
                {
                    // Trả về payUrl để Client/Frontend redirect
                    return Ok(new { payUrl = momoResponse.payUrl });
                }
                return BadRequest(momoResponse.message);
            }
            catch (Exception ex)

            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// API 2: MoMo Server gọi để thông báo kết quả (IPN)
        /// </summary>
        [HttpPost("momo/ipn")]
        public async Task<IActionResult> MoMoIpnHandler([FromBody] MomoIpnRequest request)
        {
            _logger.LogInformation("Nhận được IPN từ MoMo, OrderId: {OrderId}, ResultCode: {ResultCode}",
                request.orderId,
                request.resultCode);

            _logger.LogDebug(JsonSerializer.Serialize(request));

            try
            {
                await _paymentService.ProcessMoMoIpnAsync(request);

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
    }
    public class CheckoutRequestDto
    {
        // public string StudentId { get; set; }
        public List<string> CourseIds { get; set; }
    }
}