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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using OrderingService.Application.Interfaces;

namespace OrderingService.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Course>> GetCoursesByIdsAsync(List<string> courseIds)
        {
            return await _context.Courses
                .Where(c => courseIds.Contains(c.Id))
                .ToListAsync();
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            return order;
        }

        public async Task AddOrderItemsAsync(List<OrderItem> orderItems)
        {
            await _context.OrderItems.AddRangeAsync(orderItems);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(string orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<OrderItem>> GetOrderItemsByOrderIdAsync(string orderId)
        {
            return await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();
        }
        public async Task AddTransactionAsync(PaymentTransaction transaction)
        {
            await _context.PaymentTransactions.AddAsync(transaction);
        }

        public async Task UpdateOrderStatusAsync(string orderId, string status, DateTime? paidAt)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order != null)
            {
                order.Status = status;
                if (paidAt.HasValue)
                {
                    order.PaidAt = paidAt;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Enrollment>> GetEnrollmentsByStudentAndCoursesAsync(string studentId, List<string> courseIds)
        {
            return await _context.Enrollments
                .Where(e => e.StudentId == studentId && courseIds.Contains(e.CourseId) && e.Status == true)
                .ToListAsync();
        }

        public async Task<GiftCode?> GetGiftCodeByCodeAsync(string code)
        {
            return await _context.GiftCodes
                .Include(gc => gc.Course)
                .FirstOrDefaultAsync(gc => gc.Code == code);
        }

        public async Task<GiftCode?> GetGiftCodeByCodeAndCourseAsync(string code, string? courseId)
        {
            return await _context.GiftCodes
                .Include(gc => gc.Course)
                .FirstOrDefaultAsync(gc => gc.Code == code && gc.CourseId == courseId);
        }

        public async Task<GiftCode?> GetGiftCodeByIdAsync(string id)
        {
            return await _context.GiftCodes
                .Include(gc => gc.Course)
                .FirstOrDefaultAsync(gc => gc.Id == id);
        }

        public async Task<bool> CheckGiftCodeDuplicateAsync(string code, string? courseId)
        {
            return await _context.GiftCodes.AnyAsync(gc => gc.Code == code && gc.CourseId == courseId);
        }

        public async Task<List<GiftCode>> GetGiftCodesByCourseAsync(string courseId)
        {
            return await _context.GiftCodes
                .Where(gc => gc.CourseId == courseId)
                .OrderByDescending(gc => gc.CreatedAt)
                .ToListAsync();
        }

        public async Task AddGiftCodeAsync(GiftCode giftCode)
        {
            await _context.GiftCodes.AddAsync(giftCode);
        }

        public async Task DeleteGiftCodeAsync(GiftCode giftCode)
        {
            _context.GiftCodes.Remove(giftCode);
            await Task.CompletedTask;
        }

        public async Task AddEnrollmentAsync(Enrollment enrollment)
        {
            await _context.Enrollments.AddAsync(enrollment);
        }

        public async Task<Course?> GetCourseByIdAsync(string courseId)
        {
            return await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        }

        public async Task<(List<Order> items, int totalCount)> GetOrdersByStudentIdAsync(string studentId, int pageNumber, int pageSize)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Course)
                .Include(o => o.PaymentTransactions)
                .Where(o => o.StudentId == studentId);

            int totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task RemoveFromWishlistAsync(string studentId, List<string> courseIds)
        {
            var wishlistItems = await _context.Wishlists
                .Where(w => w.StudentId == studentId && courseIds.Contains(w.CourseId))
                .ToListAsync();

            if (wishlistItems.Any())
            {
                _context.Wishlists.RemoveRange(wishlistItems);
            }
        }
    }
}


