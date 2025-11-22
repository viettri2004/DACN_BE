using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Interfaces;

namespace PaymentService.Infrastructure.Repositories
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
    }
}