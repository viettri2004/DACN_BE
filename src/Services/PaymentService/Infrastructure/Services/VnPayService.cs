using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;
using CourseService.Application.Interfaces;
using AccountService.Application.Interfaces;
using AccountService.Domain.Enums;
using Shared.Domain.Entities;
using src.Shared.Resources;
using PaymentService.Infrastructure.Services.Helpers;
using Hangfire;

namespace PaymentService.Infrastructure.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly ILogger<VnPayService> _logger;
        private readonly IDistributedCache _cache;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILuceneSearchService _luceneSearchService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public VnPayService(IConfiguration config, AppDbContext context, ILogger<VnPayService> logger, IDistributedCache cache, IBackgroundJobClient backgroundJobClient, ILuceneSearchService luceneSearchService, INotificationRepository notificationRepository, IStringLocalizer<SharedResources> localizer)
        {
            _config = config;
            _context = context;
            _logger = logger;
            _cache = cache;
            _backgroundJobClient = backgroundJobClient;
            _luceneSearchService = luceneSearchService;
            _notificationRepository = notificationRepository;
            _localizer = localizer;
        }

        public string CreatePaymentUrl(HttpContext context, VnPayPaymentRequestModel model)
        {
            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", _config["VnPay:TmnCode"]);
            vnpay.AddRequestData("vnp_Amount", ((long)model.Amount * 100).ToString());

            TimeZoneInfo timeZoneId;

            try
            {
                timeZoneId = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                timeZoneId = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }

            DateTime timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneId);

            vnpay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));

            // Thêm ExpireDate (thường là +15 phút) để tránh lỗi timeout
            vnpay.AddRequestData("vnp_ExpireDate", timeNow.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            vnpay.AddRequestData("vnp_CurrCode", "VND");

            string clientIpAddress = Utils.GetIpAddress(context);
            if (clientIpAddress == "::1" || string.IsNullOrEmpty(clientIpAddress))
            {
                clientIpAddress = "127.0.0.1";
            }
            vnpay.AddRequestData("vnp_IpAddr", clientIpAddress);

            vnpay.AddRequestData("vnp_Locale", "vn");

            // 4. Thông tin đơn hàng (Tiếng Việt KHÔNG DẤU, không ký tự đặc biệt)
            // "Thanh toan don hang:" + model.OrderId có thể chứa ký tự lạ nếu OrderId là chuỗi phức tạp
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + model.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other");

            vnpay.AddRequestData("vnp_ReturnUrl", _config["VnPay:ReturnUrl"]);

            // LƯU Ý: Thường không gửi vnp_IpnUrl qua API, bạn cấu hình trên trang quản trị VNPAY.
            // Nếu gửi mà bên kia không nhận => Sai checksum => Lỗi 99.
            // vnpay.AddRequestData("vnp_IpnUrl", _config["VnPay:IpnUrl"]); // Comment dòng này lại thử xem

            vnpay.AddRequestData("vnp_TxnRef", model.OrderId);

            return vnpay.CreateRequestUrl(_config["VnPay:BaseUrl"], Environment.GetEnvironmentVariable("VnPay__HashSecret"));
        }

        public VnPayPaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();

            // Lấy toàn bộ dữ liệu trả về từ VNPAY
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            // Lấy các tham số quan trọng
            var vnp_orderId = vnpay.GetResponseData("vnp_TxnRef");
            var vnp_TransactionId = vnpay.GetResponseData("vnp_TransactionNo");
            var vnp_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_Amount = vnpay.GetResponseData("vnp_Amount");

            // Kiểm tra chữ ký bảo mật
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, Environment.GetEnvironmentVariable("VnPay__HashSecret"));

            if (!checkSignature)
            {
                return new VnPayPaymentResponseModel
                {
                    Success = false,
                    VnPayResponseCode = "InvalidSignature"
                };
            }

            return new VnPayPaymentResponseModel
            {
                Success = true,
                PaymentMethod = "VnPay",
                OrderId = vnp_orderId,
                TransactionId = vnp_TransactionId,
                Token = vnp_SecureHash,
                VnPayResponseCode = vnp_ResponseCode,
                Amount = !string.IsNullOrEmpty(vnp_Amount) ? decimal.Parse(vnp_Amount) / 100 : 0
            };
        }

        public async Task ProcessVnPayIpnAsync(VnPayPaymentResponseModel response)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == response.OrderId);

                if (order == null)
                {
                    _logger.LogError("Order not found for VnPay IPN: {OrderId}", response.OrderId);
                    return;
                }

                if (order.Status == "Paid")
                {
                    _logger.LogInformation("Order {OrderId} already paid, skipping VnPay IPN processing", response.OrderId);
                    return;
                }

                var paymentTransaction = new PaymentTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    GatewayTransactionId = response.TransactionId,
                    GatewayToken = response.Token,
                    Amount = response.Amount,
                    PaymentStatus = response.VnPayResponseCode == "00" ? "Success" : "Failed",
                    TransactionDate = DateTime.UtcNow,
                    GatewayResponse = "VnPay",
                    ErrorCode = response.VnPayResponseCode
                };

                await _context.PaymentTransactions.AddAsync(paymentTransaction);

                if (response.VnPayResponseCode == "00")
                {
                    order.Status = "Paid";
                    order.PaidAt = DateTime.UtcNow;
                    order.PaymentMethod = "VnPay";

                    var orderItems = order.OrderItems ?? new List<OrderItem>();
                    var courseIds = orderItems.Select(oi => oi.CourseId).ToList();

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

                        _logger.LogInformation("Invalidated Redis cart cache and removed {Count} wishlist items for student {StudentId} via VnPay", wishlistItems.Count, order.StudentId);
                    }

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
                }
                else
                {
                    order.Status = "Failed";
                }

                await _context.SaveChangesAsync();

                // Clear personalized recommendation cache for this user
                if (response.VnPayResponseCode == "00" && !string.IsNullOrEmpty(order.StudentId))
                {
                    await _cache.RemoveAsync($"course:recommended:user:{order.StudentId}");
                }

                await dbTransaction.CommitAsync();

                // Re-index courses to update student count
                if (response.VnPayResponseCode == "00")
                {
                    var orderItems = order.OrderItems ?? new List<OrderItem>();
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
                            _logger.LogError(ex, "Failed to re-index course {CourseId} after VnPay payment", item.CourseId);
                        }
                }

                    // Trigger NewEnrollment notification for instructors (smart: only if no unread exists)
                    var student = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == order.StudentId);
                    foreach (var item in orderItems)
                    {
                        try
                        {
                            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == item.CourseId);
                            if (course != null)
                            {
                                var hasUnread = await _context.Notifications
                                    .AnyAsync(n => n.UserId == course.InstructorId
                                                && n.Type == NotificationType.NewEnrollment
                                                && !n.IsRead);
                                if (!hasUnread)
                                {
                                    await _notificationRepository.CreateNotificationAsync(new Notification
                                    {
                                        UserId = course.InstructorId,
                                        Title = _localizer["NewEnrollmentNotifTitle"].Value,
                                        Message = string.Format(_localizer["NewEnrollmentNotifMessage"].Value,
                                            student?.FullName ?? "Student", course.Name),
                                        Type = NotificationType.NewEnrollment,
                                        CreatedAt = DateTime.UtcNow
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "NewEnrollment notification failed for course {CourseId}", item.CourseId);
                        }
                    }

                    // Student notification for payment success
                    try
                    {
                        foreach (var item in orderItems)
                        {
                            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == item.CourseId);
                            if (course != null)
                            {
                                await _notificationRepository.CreateNotificationAsync(new Notification
                                {
                                    UserId = order.StudentId,
                                    Title = _localizer["PaymentSuccessNotifTitle"].Value,
                                    Message = string.Format(_localizer["PaymentSuccessNotifMessage"].Value, course.Name),
                                    Type = NotificationType.System,
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "PaymentSuccess notification failed for student {StudentId}", order.StudentId);
                    }
                }

                _logger.LogInformation("Processed VnPay IPN for Order {OrderId} with status {Status}", response.OrderId, order.Status);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error processing IPN for Order {OrderId}", response.OrderId);
                throw;
            }
        }
    }
}