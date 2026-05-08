using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.DTOs;
using OrderingService.Domain.Entities;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using LearningService.Application.Services;
using LearningService.Application.Interfaces;
using LearningService.Domain.Entities;
using InteractionService.Application.DTOs;
using InteractionService.Application.Interfaces;
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using OrderingService.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderingService.Application.Interfaces
{
    public interface IPaymentRepository
    {
        Task<List<Course>> GetCoursesByIdsAsync(List<string> courseIds);
        Task<Order> CreateOrderAsync(Order order);
        Task AddOrderItemsAsync(List<OrderItem> orderItems);
        Task SaveChangesAsync();
        Task<Order?> GetOrderByIdAsync(string orderId);
        Task<List<OrderItem>> GetOrderItemsByOrderIdAsync(string orderId);
        Task AddTransactionAsync(PaymentTransaction transaction);
        Task UpdateOrderStatusAsync(string orderId, string status, DateTime? paidAt);
        Task<List<Enrollment>> GetEnrollmentsByStudentAndCoursesAsync(string studentId, List<string> courseIds);
        Task<GiftCode?> GetGiftCodeByCodeAsync(string code);
        Task<GiftCode?> GetGiftCodeByCodeAndCourseAsync(string code, string? courseId);
        Task<GiftCode?> GetGiftCodeByIdAsync(string id);
        Task<bool> CheckGiftCodeDuplicateAsync(string code, string? courseId);
        Task<List<GiftCode>> GetGiftCodesByCourseAsync(string courseId);
        Task AddGiftCodeAsync(GiftCode giftCode);
        Task DeleteGiftCodeAsync(GiftCode giftCode);
        Task AddEnrollmentAsync(Enrollment enrollment);
        Task<Course?> GetCourseByIdAsync(string courseId);
        Task<(List<Order> items, int totalCount)> GetOrdersByStudentIdAsync(string studentId, int pageNumber, int pageSize);
        Task<User?> GetUserByIdAsync(string userId);
        Task RemoveFromWishlistAsync(string studentId, List<string> courseIds);
    }
}



