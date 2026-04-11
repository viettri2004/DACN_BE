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
using Hangfire;

namespace CartService.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IDistributedCache _cache;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public CartRepository(AppDbContext context, 
                             IStringLocalizer<SharedResources> localizer, 
                             IDistributedCache cache,
                             IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            _localizer = localizer;
            _cache = cache;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<ApiResponse> AddToCartAsync(string courseId, string studentId)
        {
            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null)
                return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);

            var isEnrolled = await _context.Enrollments.AsNoTracking()
                .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.Status == true);

            if (isEnrolled)
                return new ApiResponse("Conflict", _localizer["AlreadyEnrolled"].Value, null, false);

            var cartResponse = await GetAllItemsAsync(studentId);
            var cartDto = cartResponse.Data as CartDTO ?? new CartDTO();

            if (cartDto.Items.Any(i => i.Id == courseId))
                return new ApiResponse("Conflict", _localizer["ItemAlreadyInCart"].Value, null, false);

            if (cartDto.TotalItems >= 5)
                return new ApiResponse("Conflict", _localizer["CartFull"].Value, null, false);

            var newItem = new CartItemDTO
            {
                Id = course.Id,
                Name = course.Name,
                ImageUrl = course.ImageUrl,
                InstructorName = (await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == course.InstructorId))?.FullName ?? "Unknown",
                Price = course.Price,
                OriginalPrice = null,
                IsBestseller = (await _context.Enrollments.AsNoTracking().CountAsync(e => e.CourseId == course.Id)) > 5
            };

            cartDto.Items.Add(newItem);
            cartDto.TotalItems = cartDto.Items.Count;
            cartDto.TotalPrice = cartDto.Items.Sum(i => i.Price);
            await UpdateCartCache(studentId, cartDto);

            await ScheduleSyncJobAsync(studentId);

            return new ApiResponse("Success", _localizer["ItemAddedToCart"].Value, null, true);
        }

        public async Task<ApiResponse> RemoveFromCartAsync(string courseId, string studentId)
        {
            var cartResponse = await GetAllItemsAsync(studentId);
            var cartDto = cartResponse.Data as CartDTO;

            if (cartDto == null || !cartDto.Items.Any(i => i.Id == courseId))
                return new ApiResponse("NotFound", _localizer["ItemNotFoundInCart"].Value, null, false);

            cartDto.Items.RemoveAll(i => i.Id == courseId);
            cartDto.TotalItems = cartDto.Items.Count;
            cartDto.TotalPrice = cartDto.Items.Sum(i => i.Price);

            await UpdateCartCache(studentId, cartDto);

            await ScheduleSyncJobAsync(studentId);

            return new ApiResponse("Success", _localizer["ItemRemovedFromCart"].Value, null, true);
        }

        public async Task<ApiResponse> GetAllItemsAsync(string studentId)
        {
            string cacheKey = $"cart:{studentId}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(cachedData, JsonSettings.CamelCase);
                if (apiResponse != null && apiResponse.Data != null)
                {
                    if (apiResponse.Data is Newtonsoft.Json.Linq.JObject jObject)
                    {
                        apiResponse.Data = jObject.ToObject<CartDTO>();
                    }
                }
                return apiResponse;
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
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // Tăng TTL để tránh mất dữ liệu khi chưa kịp sync
            };
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response, JsonSettings.CamelCase), cacheOptions);

            return response;
        }

        public async Task SyncCartToDbAsync(string studentId)
        {
            var cartResponse = await GetAllItemsAsync(studentId);
            var cartDto = cartResponse.Data as CartDTO;

            if (cartDto == null) return;

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

            var oldItems = _context.CartItems.Where(ci => ci.CartId == cart.Id);
            _context.CartItems.RemoveRange(oldItems);

            foreach (var item in cartDto.Items)
            {
                await _context.CartItems.AddAsync(new CartItem
                {
                    Id = Guid.NewGuid().ToString(),
                    CartId = cart.Id,
                    CourseId = item.Id,
                    Price = item.Price
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task ScheduleSyncJobAsync(string studentId)
        {
            string jobCacheKey = $"cart:syncjob:{studentId}";
            var existingJobId = await _cache.GetStringAsync(jobCacheKey);

            if (!string.IsNullOrEmpty(existingJobId))
            {
                _backgroundJobClient.Delete(existingJobId);
            }

            var newJobId = _backgroundJobClient.Schedule<ICartRepository>(repo => repo.SyncCartToDbAsync(studentId), TimeSpan.FromMinutes(15));

            if (!string.IsNullOrEmpty(newJobId))
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                };
                await _cache.SetStringAsync(jobCacheKey, newJobId, cacheOptions);
            }
        }

        private async Task UpdateCartCache(string studentId, CartDTO cartDto)
        {
            string cacheKey = $"cart:{studentId}";
            var response = new ApiResponse("Success", _localizer["Success"].Value, cartDto, true);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response, JsonSettings.CamelCase), cacheOptions);
        }
    }
}