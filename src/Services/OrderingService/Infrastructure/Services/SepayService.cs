using SearchService.Application.DTOs;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Domain.Entities;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Entities;
using LearningService.Application.Services;
using LearningService.Application.Interfaces;
using LearningService.Domain.Entities;
using InteractionService.Application.DTOs;
using InteractionService.Application.Interfaces;
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Repositories;
using SearchService.Application.Services;
using SearchService.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using ContentService.Application.Interfaces;
using IdentityService.Application.Interfaces;
using Shared.Domain.Entities;
using src.Shared.Resources;
using Hangfire;

namespace OrderingService.Infrastructure.Services
{
    public class SepayService : ISepayService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SepayService> _logger;
        private readonly IDistributedCache _cache;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILuceneSearchService _luceneSearchService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IConfiguration _configuration;

        public SepayService(AppDbContext context, ILogger<SepayService> logger, IDistributedCache cache, IBackgroundJobClient backgroundJobClient, ILuceneSearchService luceneSearchService, INotificationRepository notificationRepository, IStringLocalizer<SharedResources> localizer, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _backgroundJobClient = backgroundJobClient;
            _luceneSearchService = luceneSearchService;
            _notificationRepository = notificationRepository;
            _localizer = localizer;
            _configuration = configuration;
        }

        public async Task ProcessSepayWebhookAsync(SepayWebhookRequest request)
        {
            // Security Check: verify API Key if provided in request (assuming it's passed or available via config)
            // Note: In a real scenario, you'd check the Authorization header in the Controller and pass the result here,
            // or inject IHttpContextAccessor to check headers here.
            
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
            var courseIds = orderItems.Select(oi => oi.CourseId).ToList();

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

            // Remove items from cart and wishlist
            if (!string.IsNullOrEmpty(order.StudentId))
            {
                // Remove from Wishlist
                var wishlistItems = await _context.Wishlists
                    .Where(w => w.StudentId == order.StudentId && courseIds.Contains(w.CourseId))
                    .ToListAsync();
                
                if (wishlistItems.Any())
                {
                    _context.Wishlists.RemoveRange(wishlistItems);
                }

                // Clear Redis cache and cancel pending sync job
                await _cache.RemoveAsync($"cart:{order.StudentId}");
                var jobCacheKey = $"cart:syncjob:{order.StudentId}";
                var jobId = await _cache.GetStringAsync(jobCacheKey);
                if (!string.IsNullOrEmpty(jobId))
                {
                    _backgroundJobClient.Delete(jobId);
                    await _cache.RemoveAsync(jobCacheKey);
                }

                _logger.LogInformation("Invalidated Redis cart cache and removed {Count} wishlist items for student {StudentId} via Sepay", wishlistItems.Count, order.StudentId);
            }

            await _context.SaveChangesAsync();

            // Clear personalized recommendation cache for this user
            if (!string.IsNullOrEmpty(order.StudentId))
            {
                await _cache.RemoveAsync($"course:recommended:user:{order.StudentId}");
            }

            // Re-index courses to update student count
            
            // Batch fetch courses for indexing and notifications
            var coursesForIndex = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Instructor)
                .Include(c => c.CourseTags)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Comments)
                .AsSplitQuery()
                .Where(c => courseIds.Contains(c.Id))
                .ToListAsync();

            foreach (var course in coursesForIndex)
            {
                try
                {
                    await _luceneSearchService.IndexCourseAsync(course);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to re-index course {CourseId} after Sepay payment", course.Id);
                }
            }

            _logger.LogInformation("Kích hoạt thành công Order {OrderId} qua Sepay Webhook.", orderId);

            // Trigger NewEnrollment notification for instructors
            var student = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == order.StudentId);
            var instructorIds = coursesForIndex.Select(c => c.InstructorId).Distinct().ToList();
            
            // Check unread status in batch
            var instructorsWithUnread = await _context.Notifications
                .Where(n => instructorIds.Contains(n.UserId) 
                            && n.Type == NotificationType.NewEnrollment 
                            && !n.IsRead)
                .Select(n => n.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var course in coursesForIndex)
            {
                try
                {
                    if (!instructorsWithUnread.Contains(course.InstructorId))
                    {
                        await _notificationRepository.CreateNotificationAsync(new Notification
                        {
                            UserId = course.InstructorId,
                            Title = _localizer["NewEnrollmentNotifTitle"].Value,
                            Message = string.Format(_localizer["NewEnrollmentNotifMessage"].Value,
                                student?.FullName ?? "Student", course.Name),
                            Type = NotificationType.NewEnrollment,
                            CreatedAt = DateTime.UtcNow,
                            RelatedId = course.Id
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "NewEnrollment notification failed for course {CourseId}", course.Id);
                }
            }

            // Student notification for payment success
            try
            {
                foreach (var course in coursesForIndex)
                {
                    await _notificationRepository.CreateNotificationAsync(new Notification
                    {
                        UserId = order.StudentId,
                        Title = _localizer["PaymentSuccessNotifTitle"].Value,
                        Message = string.Format(_localizer["PaymentSuccessNotifMessage"].Value, course.Name),
                        Type = NotificationType.System,
                        CreatedAt = DateTime.UtcNow,
                        RelatedId = course.Id
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentSuccess notification failed for student {StudentId}", order.StudentId);
            }
        }
    }
}



