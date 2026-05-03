using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Domain.Entities;
using CourseService.Domain.Enums;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using CartService.Application.Interfaces;

namespace CourseService.Application.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public WishlistService(IWishlistRepository wishlistRepository, ICartRepository cartRepository, IStringLocalizer<SharedResources> localizer)
        {
            _wishlistRepository = wishlistRepository;
            _cartRepository = cartRepository;
            _localizer = localizer;
        }

        public async Task<ApiResponse> AddToWishlistAsync(string courseId, string studentId)
        {
            var exists = await _wishlistRepository.GetWishlistItemAsync(studentId, courseId);
            if (exists != null) 
                return new ApiResponse("Conflict", _localizer["AlreadyInWishlist"].Value, null, false);

            var wishlist = new Wishlist
            {
                Id = Guid.NewGuid().ToString(),
                StudentId = studentId,
                CourseId = courseId,
                AddedAt = DateTime.UtcNow
            };

            await _wishlistRepository.AddAsync(wishlist);
            await _wishlistRepository.SaveChangesAsync();

            // When adding to wishlist, remove from cart
            await _cartRepository.RemoveFromCartAsync(courseId, studentId);

            return new ApiResponse("Created", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> RemoveFromWishlistAsync(string courseId, string studentId)
        {
            var wishlist = await _wishlistRepository.GetWishlistItemAsync(studentId, courseId);
            if (wishlist == null) 
                return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            await _wishlistRepository.RemoveAsync(wishlist);
            await _wishlistRepository.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> GetStudentWishlistAsync(string studentId, int pageNumber, int pageSize)
        {
            var query = _wishlistRepository.GetWishlistQueryable()
                .AsNoTracking()
                .Where(w => w.StudentId == studentId);

            var totalCount = await query.CountAsync();
            var result = await query
                .OrderByDescending(w => w.AddedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new CourseCardDTO
                {
                    Id = w.Course.Id,
                    Name = w.Course.Name,
                    Description = w.Course.Description,
                    ImageUrl = w.Course.ImageUrl,
                    InstructorName = w.Course.Instructor.FullName,
                    AverageRating = w.Course.Enrollments
                        .SelectMany(e => e.Comments)
                        .Where(cm => cm.Type == CommentType.Review)
                        .Average(cm => (double?)cm.Rate) ?? 0,
                    TotalReviews = w.Course.Enrollments
                        .SelectMany(e => e.Comments)
                        .Count(cm => cm.Type == CommentType.Review),
                    TotalStudents = w.Course.Enrollments.Count,
                    Price = w.Course.Price,
                    OriginalPrice = w.Course.Price * 1.2m,
                    TotalHours = (int)Math.Ceiling(w.Course.Lectures.SelectMany(l => l.LectureVideos).Sum(v => v.Duration) / 3600.0),
                    IsBestseller = w.Course.Enrollments.Count > 50,
                    LastUpdate = w.Course.UpdatedAt
                })
                .ToListAsync();

            // Round ratings in memory for cleaner response
            foreach (var item in result)
            {
                item.AverageRating = Math.Round(item.AverageRating, 1);
            }

            return new ApiResponse("Success", _localizer["Success"].Value, new PagedResult<CourseCardDTO> 
            { 
                Items = result, 
                Page = pageNumber, 
                PageSize = pageSize, 
                TotalCount = totalCount 
            }, true);
        }
    }
}
