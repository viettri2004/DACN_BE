using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;

namespace PaymentService.Application.Interfaces
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
        Task<List<Order>> GetOrdersByStudentIdAsync(string studentId);
        Task<User?> GetUserByIdAsync(string userId);
    }
}