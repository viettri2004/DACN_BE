using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using AccountService.Application.Interfaces;
using Entities;
using Microsoft.Extensions.Localization;
using src.Shared.Resources;
using src.Shared.Domain.Entities;
using AccountService.Application.DTOs;
using AccountService.Domain.Enums;

namespace AccountService.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public NotificationRepository(AppDbContext context, IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<ApiResponse> CreateNotificationAsync(Notification notification)
        {
            try
            {
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return new ApiResponse("Created", _localizer["Success"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> GetUserNotificationsAsync(string userId, List<string> roles)
        {
            try
            {
                var allNotifications = await _context.Notifications
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                // Client-side filtering because of enum to string issues in some EF providers with Contains
                var notifications = allNotifications
                    .Where(n => n.UserId == userId || (n.Role != null && roles.Contains(n.Role.ToString()!)))
                    .ToList();

                var notificationDtos = notifications.Select(n => new NotificationDTO
                {
                    Id = n.Id,
                    Type = n.Type.ToString(),
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList();

                return new ApiResponse("Success", _localizer["Success"].Value, notificationDtos, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> MarkAsReadAsync(string notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();
                }
                return new ApiResponse("Success", _localizer["Success"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> MarkAllAsReadAsync(string userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var n in notifications)
                {
                    n.IsRead = true;
                }

                await _context.SaveChangesAsync();
                return new ApiResponse("Success", _localizer["Success"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
    }
}
