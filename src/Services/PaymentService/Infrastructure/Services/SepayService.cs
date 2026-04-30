using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;
using CourseService.Application.Interfaces;
using Hangfire;

namespace PaymentService.Infrastructure.Services
{
    public class SepayService : ISepayService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SepayService> _logger;
        private readonly IDistributedCache _cache;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILuceneSearchService _luceneSearchService;

        public SepayService(AppDbContext context, ILogger<SepayService> logger, IDistributedCache cache, IBackgroundJobClient backgroundJobClient, ILuceneSearchService luceneSearchService)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _backgroundJobClient = backgroundJobClient;
            _luceneSearchService = luceneSearchService;
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

            // Remove items from cart and cancel pending sync job
            if (!string.IsNullOrEmpty(order.StudentId))
            {
                // Clear Redis cache and cancel pending sync job before DB changes to ensure consistency
                await _cache.RemoveAsync($"cart:{order.StudentId}");
                var jobCacheKey = $"cart:syncjob:{order.StudentId}";
                var jobId = await _cache.GetStringAsync(jobCacheKey);
                if (!string.IsNullOrEmpty(jobId))
                {
                    _backgroundJobClient.Delete(jobId);
                    await _cache.RemoveAsync(jobCacheKey);
                }

                var courseIds = orderItems.Select(oi => oi.CourseId).ToList();
                if (courseIds.Any())
                {
                    _logger.LogInformation("Cart items will be invalidated from Redis for student {StudentId} via Sepay", order.StudentId);
                }
            }

            await _context.SaveChangesAsync();

            // Re-index courses to update student count
            foreach (var item in orderItems)
            {
                try
                {
                    var courseForIndex = await _context.Courses
                        .AsNoTracking()
                        .Include(c => c.Instructor)
                        .Include(c => c.CourseTags)
                        .Include(c => c.Enrollments)
                            .ThenInclude(e => e.Comments)
                        .FirstOrDefaultAsync(c => c.Id == item.CourseId);

                    if (courseForIndex != null)
                    {
                        await _luceneSearchService.IndexCourseAsync(courseForIndex);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to re-index course {CourseId} after Sepay payment", item.CourseId);
                }
            }

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