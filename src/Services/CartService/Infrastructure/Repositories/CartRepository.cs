using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CartService.Application.DTOs;
using CartService.Application.Interfaces;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using src.Shared.Infrastructure;

using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace CartService.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IDistributedCache _cache;

        public CartRepository(AppDbContext context, IStringLocalizer<SharedResources> localizer, IDistributedCache cache)
        {
            _context = context;
            _localizer = localizer;
            _cache = cache;
        }

        public async Task<ApiResponse> AddToCartAsync(string courseId, string studentId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);
            }

            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.Status == true);

            if (isEnrolled)
            {
                return new ApiResponse("Conflict", _localizer["AlreadyEnrolled"].Value, null, false);
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.StudentId == studentId);
            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = studentId
                };
                await _context.Carts.AddAsync(cart);
            }

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.CourseId == courseId);

            if (existingItem != null)
            {
                return new ApiResponse("Conflict", _localizer["ItemAlreadyInCart"].Value, null, false);
            }

            var currentItemCount = await _context.CartItems.CountAsync(ci => ci.CartId == cart.Id);
            if (currentItemCount >= 5)
            {
                return new ApiResponse("Conflict", _localizer["CartFull"].Value, null, false);
            }

            var cartItem = new CartItem
            {
                Id = Guid.NewGuid().ToString(),
                CartId = cart.Id,
                CourseId = courseId,
                Price = course.Price
            };

            await _context.CartItems.AddAsync(cartItem);
            await _context.SaveChangesAsync();
            await RemoveCartCache(studentId);

            return new ApiResponse("Success", _localizer["ItemAddedToCart"].Value, null, true);
        }

        public async Task<ApiResponse> RemoveFromCartAsync(string courseId, string studentId)
        {
            var cart = await _context.Carts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.StudentId == studentId);

            if (cart == null)
            {
                return new ApiResponse("NotFound", _localizer["ItemNotFoundInCart"].Value, null, false);
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.CourseId == courseId);

            if (cartItem == null)
            {
                return new ApiResponse("NotFound", _localizer["ItemNotFoundInCart"].Value, null, false);
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            await RemoveCartCache(studentId);

            return new ApiResponse("Success", _localizer["ItemRemovedFromCart"].Value, null, true);
        }

        public async Task<ApiResponse> GetAllItemsAsync(string studentId)
        {
            string cacheKey = $"cart:{studentId}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<ApiResponse>(cachedData, JsonSettings.CamelCase);
            }

            var cart = await _context.Carts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.StudentId == studentId);

            var cartDto = new CartDTO();

            if (cart == null)
            {
                return new ApiResponse("Success", _localizer["CartIsEmpty"].Value, cartDto, true);
            }
            var cartItemsQuery = _context.CartItems
                .AsNoTracking()
                .Where(ci => ci.CartId == cart.Id)
                .Include(ci => ci.Course)
                    .ThenInclude(c => c.Instructor)
                .Include(ci => ci.Course)
                    .ThenInclude(c => c.Enrollments)
                        .ThenInclude(e => e.Comments);

            var items = await cartItemsQuery
                .Select(ci => new CartItemDTO
                {
                    Id = ci.Course.Id,
                    Name = ci.Course.Name,
                    ImageUrl = ci.Course.ImageUrl,
                    InstructorName = ci.Course.Instructor.FullName,

                    AverageRating = ci.Course.Enrollments.SelectMany(e => e.Comments).Any()
                        ? Math.Round(ci.Course.Enrollments.SelectMany(e => e.Comments).Average(cm => cm.Rate), 1)
                        : 0,
                    TotalReviews = ci.Course.Enrollments.SelectMany(e => e.Comments).Count(),
                    TotalStudents = ci.Course.Enrollments.Count,

                    Price = ci.Price,

                    OriginalPrice = (ci.Price < ci.Course.Price) ? ci.Course.Price : null,

                    IsBestseller = ci.Course.Enrollments.Count > 5,
                })
                .ToListAsync();

            cartDto.Items = items;
            cartDto.TotalItems = items.Count;
            cartDto.TotalPrice = items.Sum(item => item.Price);

            var response = new ApiResponse("Success", _localizer["Success"].Value, cartDto, true);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            };
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response, JsonSettings.CamelCase), cacheOptions);

            return response;
        }

        private async Task RemoveCartCache(string studentId)
        {
            await _cache.RemoveAsync($"cart:{studentId}");
        }
    }
}