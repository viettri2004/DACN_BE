using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using src.Shared.Infrastructure;

using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace OrderingService.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IDistributedCache _cache;

        public CartRepository(AppDbContext context, 
                             IStringLocalizer<SharedResources> localizer, 
                             IDistributedCache cache)
        {
            _context = context;
            _localizer = localizer;
            _cache = cache;
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

            // If item is in wishlist, remove it
            var wishlistItem = await _context.Wishlists.FirstOrDefaultAsync(w => w.StudentId == studentId && w.CourseId == courseId);
            if (wishlistItem != null)
            {
                _context.Wishlists.Remove(wishlistItem);
                await _context.SaveChangesAsync();
            }

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

            // If not in cache, return an empty cart since we no longer store it in DB
            var cartDto = new CartDTO();
            return new ApiResponse("Success", _localizer["CartIsEmpty"].Value, cartDto, true);
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


