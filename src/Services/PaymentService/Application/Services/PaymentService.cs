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
using Hangfire;
using CourseService.Application.Interfaces;
using Data.Context;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IVnPayService _vnPayService;
        private readonly BankConfig _bankConfig;
        private readonly IDistributedCache _cache;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILuceneSearchService _luceneSearchService;
        private readonly AppDbContext _context;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IVnPayService vnPayService,
            IConfiguration configuration,
            IDistributedCache cache,
            IStringLocalizer<SharedResources> localizer,
            IBackgroundJobClient backgroundJobClient,
            ILuceneSearchService luceneSearchService,
            AppDbContext context)
        {
            _paymentRepository = paymentRepository;
            _vnPayService = vnPayService;
            _bankConfig = configuration.GetSection("Bank").Get<BankConfig>() ?? new BankConfig();
            _cache = cache;
            _localizer = localizer;
            _backgroundJobClient = backgroundJobClient;
            _luceneSearchService = luceneSearchService;
            _context = context;
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

        public async Task<ApiResponse> CreateGiftCodeAsync(CreateGiftCodeDto createDto, string userId)
        {
            try
            {
                var user = await _paymentRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);
                }

                // Check authorization: Admin or Course Owner
                if (user is not Admin)
                {
                    if (string.IsNullOrEmpty(createDto.CourseId))
                    {
                        return new ApiResponse("Forbidden", _localizer["UnauthorizedGiftCodeCreation"].Value, null, false);
                    }

                    var course = await _paymentRepository.GetCourseByIdAsync(createDto.CourseId);
                    if (course == null || course.InstructorId != userId)
                    {
                        return new ApiResponse("Forbidden", _localizer["UnauthorizedGiftCodeCreation"].Value, null, false);
                    }
                }

                // Code must be unique within the course OR global (null course)
                var isDuplicate = await _paymentRepository.CheckGiftCodeDuplicateAsync(createDto.Code, createDto.CourseId);
                if (isDuplicate)
                {
                    return new ApiResponse("Conflict", _localizer["GiftCodeExists"].Value, null, false);
                }

                var giftCode = new GiftCode
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = createDto.Code,
                    CourseId = createDto.CourseId,
                    ExpiryDate = createDto.ExpiryDate,
                    MaxUses = createDto.MaxUses,
                    UsageCount = 0,
                    IsActive = true,
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

        public async Task<ApiResponse> UpdateGiftCodeAsync(string giftCodeId, UpdateGiftCodeDto updateDto, string userId)
        {
            try
            {
                var giftCode = await _paymentRepository.GetGiftCodeByIdAsync(giftCodeId);
                if (giftCode == null)
                {
                    return new ApiResponse("NotFound", _localizer["GiftCodeNotFound"].Value, null, false);
                }

                var user = await _paymentRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);
                }

                // Authorization: Admin or Course Owner
                if (user is not Admin)
                {
                    if (string.IsNullOrEmpty(giftCode.CourseId))
                    {
                        return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
                    }

                    var course = await _paymentRepository.GetCourseByIdAsync(giftCode.CourseId);
                    if (course == null || course.InstructorId != userId)
                    {
                        return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
                    }
                }

                // If code is being changed, check for duplicates
                if (!string.IsNullOrEmpty(updateDto.Code) && updateDto.Code != giftCode.Code)
                {
                    var isDuplicate = await _paymentRepository.CheckGiftCodeDuplicateAsync(updateDto.Code, giftCode.CourseId);
                    if (isDuplicate)
                    {
                        return new ApiResponse("Conflict", _localizer["GiftCodeExists"].Value, null, false);
                    }
                    giftCode.Code = updateDto.Code;
                }

                if (updateDto.ExpiryDate.HasValue) giftCode.ExpiryDate = updateDto.ExpiryDate;
                if (updateDto.MaxUses.HasValue) giftCode.MaxUses = updateDto.MaxUses;
                if (updateDto.IsActive.HasValue) giftCode.IsActive = updateDto.IsActive.Value;

                await _paymentRepository.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["GiftCodeUpdated"].Value, giftCode, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> DeleteGiftCodeAsync(string giftCodeId, string userId)
        {
            try
            {
                var giftCode = await _paymentRepository.GetGiftCodeByIdAsync(giftCodeId);
                if (giftCode == null)
                {
                    return new ApiResponse("NotFound", _localizer["GiftCodeNotFound"].Value, null, false);
                }

                var user = await _paymentRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);
                }

                // Authorization: Admin or Course Owner
                if (user is not Admin)
                {
                    if (string.IsNullOrEmpty(giftCode.CourseId))
                    {
                        return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
                    }

                    var course = await _paymentRepository.GetCourseByIdAsync(giftCode.CourseId);
                    if (course == null || course.InstructorId != userId)
                    {
                        return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
                    }
                }

                await _paymentRepository.DeleteGiftCodeAsync(giftCode);
                await _paymentRepository.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["GiftCodeDeleted"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> GetGiftCodesByCourseAsync(string courseId, string userId)
        {
            try
            {
                var user = await _paymentRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);
                }

                // Check authorization: Admin or Course Owner
                if (user is not Admin)
                {
                    var course = await _paymentRepository.GetCourseByIdAsync(courseId);
                    if (course == null || course.InstructorId != userId)
                    {
                        return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
                    }
                }

                var giftCodes = await _paymentRepository.GetGiftCodesByCourseAsync(courseId);

                var result = giftCodes.Select(gc => new GiftCodeViewDto
                {
                    Id = gc.Id,
                    Code = gc.Code,
                    CourseId = gc.CourseId,
                    MaxUses = gc.MaxUses,
                    UsageCount = gc.UsageCount,
                    ExpiryDate = gc.ExpiryDate,
                    CreatedAt = gc.CreatedAt,
                    IsActive = gc.IsActive
                }).ToList();

                return new ApiResponse("Success", _localizer["GiftCodesRetrieved"].Value, result, true);
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
                // Logic: 
                // 1. Try to find a gift code specifically for this course
                // 2. If not found, try to find a general gift code (CourseId == null)
                var giftCode = await _paymentRepository.GetGiftCodeByCodeAndCourseAsync(redeemDto.Code, redeemDto.CourseId);
                
                if (giftCode == null)
                {
                    giftCode = await _paymentRepository.GetGiftCodeByCodeAndCourseAsync(redeemDto.Code, null);
                }

                if (giftCode == null || !giftCode.IsActive)
                {
                    return new ApiResponse("NotFound", _localizer["InvalidGiftCode"].Value, null, false);
                }

                // If gift code is bound to a specific course, it must match the DTO courseId (this is already covered by the query above, but keeping as safeguard)
                if (!string.IsNullOrEmpty(giftCode.CourseId) && giftCode.CourseId != redeemDto.CourseId)
                {
                    return new ApiResponse("BadRequest", _localizer["GiftCodeInvalidForCourse"].Value, null, false);
                }

                // Check usage limit
                if (giftCode.MaxUses.HasValue && giftCode.UsageCount >= giftCode.MaxUses.Value)
                {
                    return new ApiResponse("BadRequest", _localizer["GiftCodeUsageLimitReached"].Value, null, false);
                }

                if (giftCode.ExpiryDate.HasValue && giftCode.ExpiryDate.Value < DateTime.UtcNow)
                {
                    return new ApiResponse("BadRequest", _localizer["GiftCodeExpired"].Value, null, false);
                }

                // Redeem the course from DTO
                string? courseIdToRedeem = redeemDto.CourseId;

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
                
                // Track usage
                giftCode.UsageCount++;

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
                    LastVisit = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(100),
                    Status = true
                };

                await _paymentRepository.AddEnrollmentAsync(enrollment);

                // Clear cart from Redis cache and cancel pending sync job
                await _cache.RemoveAsync($"cart:{studentId}");
                var jobCacheKey = $"cart:syncjob:{studentId}";
                var jobId = await _cache.GetStringAsync(jobCacheKey);
                if (!string.IsNullOrEmpty(jobId))
                {
                    _backgroundJobClient.Delete(jobId);
                    await _cache.RemoveAsync(jobCacheKey);
                }
                
                await _paymentRepository.SaveChangesAsync();

                // Re-index to update student count
                try
                {
                    var courseForIndex = await _context.Courses
                        .AsNoTracking()
                        .Include(c => c.Instructor)
                        .Include(c => c.CourseTags)
                        .Include(c => c.Enrollments)
                            .ThenInclude(e => e.Comments)
                        .FirstOrDefaultAsync(c => c.Id == courseIdToRedeem);

                    if (courseForIndex != null)
                    {
                        await _luceneSearchService.IndexCourseAsync(courseForIndex);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Indexing failed after gift code redemption: {ex.Message}");
                }

                return new ApiResponse("Success", _localizer["GiftCodeRedeemedSuccess", course.Name].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> GetPaymentHistoryAsync(string studentId)
        {
            try
            {
                var orders = await _paymentRepository.GetOrdersByStudentIdAsync(studentId);
                var history = new List<PaymentHistoryDto>();

                foreach (var order in orders)
                {
                    string courseName = order.OrderItems.FirstOrDefault()?.Course?.Name ?? "Nạp tiền ví/Unknown";
                    string transactionId = order.PaymentTransactions.FirstOrDefault()?.GatewayTransactionId ?? order.Id;
                    
                    history.Add(new PaymentHistoryDto
                    {
                        Id = order.Id,
                        Course = order.PaymentMethod != null && order.PaymentMethod.StartsWith("GiftCode") ? "Mã Khuyến Mãi" : courseName,
                        Amount = order.TotalAmount,
                        Currency = "VND",
                        Date = order.CreatedAt,
                        Status = order.Status == "Paid" ? "Completed" : (order.Status == "Pending" ? "Pending" : "Failed"),
                        Method = order.PaymentMethod ?? "Unknown",
                        TransactionId = transactionId
                    });
                }

                return new ApiResponse("Success", _localizer["PaymentHistoryRetrieved"].Value, history, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
    }
}