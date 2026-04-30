using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CartService.Application.DTOs;
using src.Shared.Domain.Entities;
using Hangfire;

namespace CartService.Application.Interfaces
{
    public interface ICartRepository
    {
        Task<ApiResponse> AddToCartAsync(string courseId, string studentId);
        Task<ApiResponse> RemoveFromCartAsync(string courseId, string studentId);
        Task<ApiResponse> GetAllItemsAsync(string studentId);
    }
}