using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;

namespace PaymentService.Infrastructure.Services
{
    public class SepayService : ISepayService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SepayService> _logger;
        private readonly IDistributedCache _cache;

        public SepayService(AppDbContext context, ILogger<SepayService> logger, IDistributedCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task ProcessSepayWebhookAsync(SepayWebhookRequest request)
        {
            if (request.TransferType.ToLower() != "in")
            {
                _logger.LogInformation("Bỏ qua giao dịch tiền ra (out) từ Sepay.");
                return;
            }

            string? orderId = null;

            if (string.IsNullOrEmpty(request.Content))
            {
                _logger.LogError("Webhook Sepay không chứa 'content'. Bỏ qua.");
                return;
            }

            try
            {
                _logger.LogInformation("Đang bóc tách 'content': {Content}", request.Content);

                string content = request.Content;
                string marker = "Ma giao dich";

                int markerIndex = content.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

                if (markerIndex > 0)
                {
                    string parsedString = content.Substring(0, markerIndex).Trim();

                    orderId = parsedString.Replace(" ", "-");

                    _logger.LogInformation("Đã bóc tách và tái tạo OrderId: {OrderId}", orderId);
                }
                else
                {
                    _logger.LogWarning("Không tìm thấy 'Ma giao dich' trong content. Thử giả định toàn bộ content là ID.");
                    orderId = content.Trim().Replace(" ", "-");
                }   
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi bóc tách 'content': {Content}", request.Content);
                return;
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogError("Không tìm thấy Order với ID (code): {OrderId}", orderId);
                return;
            }

            if (order.Status == "Paid")
            {
                _logger.LogWarning("Order {OrderId} đã được thanh toán trước đó.", orderId);
                return;
            }

            if (request.TransferAmount < order.TotalAmount)
            {
                _logger.LogWarning("Thanh toán thiếu tiền cho Order {OrderId}. Yêu cầu: {Required}, Thực tế: {Actual}",
                    orderId, order.TotalAmount, request.TransferAmount);
                order.Status = "PartialPayment"; 
                await _context.SaveChangesAsync();
                return;
            }

            var transaction = new PaymentTransaction
            {
                Id = Guid.NewGuid().ToString(),
                OrderId = order.Id,
                GatewayTransactionId = request.ReferenceCode,
                GatewayToken = request.Id.ToString(),
                Amount = request.TransferAmount,
                PaymentStatus = "Success",
                TransactionDate = DateTime.UtcNow,
                GatewayResponse = "Sepay",
                ErrorCode = "0" 
            };
            await _context.PaymentTransactions.AddAsync(transaction);

            order.Status = "Paid";
            order.PaidAt = DateTime.UtcNow;
            order.PaymentMethod = "Sepay_MBBank";

            var orderItems = order.OrderItems ?? new List<OrderItem>();
            foreach (var item in orderItems)
            {
                var enrollment = new Enrollment
                {
                    Id = Guid.NewGuid().ToString(),
                    CourseId = item.CourseId,
                    StudentId = order.StudentId,
                    OrderId = order.Id,
                    Status = true,
                    EnrolledAt = DateTime.UtcNow,
                    LastVisit = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(100)
                };
                await _context.Enrollments.AddAsync(enrollment);
            }

            // Remove items from cart
            if (!string.IsNullOrEmpty(order.StudentId))
            {
                var courseIds = orderItems.Select(oi => oi.CourseId).ToList();
                if (courseIds.Any())
                {
                    var cartItemsToRemove = await _context.CartItems
                        .Where(ci => ci.Cart.StudentId == order.StudentId && courseIds.Contains(ci.CourseId))
                        .ToListAsync();

                    if (cartItemsToRemove.Any())
                    {
                        _context.CartItems.RemoveRange(cartItemsToRemove);
                        _logger.LogInformation("Removed {Count} items from cart for student {StudentId} via Sepay", cartItemsToRemove.Count, order.StudentId);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Invalidate Redis cart cache
            if (!string.IsNullOrEmpty(order.StudentId))
            {
                await _cache.RemoveAsync($"cart:{order.StudentId}");
                _logger.LogInformation("Invalidated Redis cart cache for student {StudentId} via Sepay", order.StudentId);
            }

            _logger.LogInformation("Kích hoạt thành công Order {OrderId} qua Sepay Webhook.", orderId);
        }
    }
}