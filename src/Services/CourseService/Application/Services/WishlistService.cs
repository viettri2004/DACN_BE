using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Domain.Entities;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace CourseService.Application.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public WishlistService(IWishlistRepository wishlistRepository, IStringLocalizer<SharedResources> localizer)
        {
            _wishlistRepository = wishlistRepository;
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
            var items = await query
                .Include(w => w.Course)
                    .ThenInclude(c => c.Instructor)
                .OrderByDescending(w => w.AddedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = items.Select(w => new CourseCardDTO
            {
                Id = w.Course.Id,
                Name = w.Course.Name,
                ImageUrl = w.Course.ImageUrl,
                InstructorName = w.Course.Instructor.FullName,
                Price = w.Course.Price
            }).ToList();

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
