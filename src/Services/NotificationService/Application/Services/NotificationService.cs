using NotificationService.Application.DTOs;
using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using OrderingService.Domain.Entities;
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
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Localization;
using src.Shared.Resources;
using src.Shared.Domain.Entities;
using IdentityService.Application.DTOs;

using Microsoft.AspNetCore.SignalR;
using Shared.Infrastructure.Hubs;
using Shared.Domain.Entities;

namespace NotificationService.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(AppDbContext context, 
                                    IStringLocalizer<SharedResources> localizer,
                                    IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _localizer = localizer;
            _hubContext = hubContext;
        }

        public async Task<ApiResponse> CreateNotificationAsync(Notification notification)
        {
            try
            {
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send Real-time notification to the User's specific group
                var dto = new NotificationDTO
                {
                    Id = notification.Id,
                    Type = notification.Type.ToString(),
                    Title = notification.Title,
                    Message = notification.Message,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt,
                    RelatedId = notification.RelatedId
                };
                
                // Using Group(userId) because we manually added users to groups named after their ID in the Hub
                await _hubContext.Clients.Group(notification.UserId).SendAsync("ReceiveNotification", dto);

                return new ApiResponse("Created", _localizer["Success"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> CreateNotificationForRoleAsync(NotificationRole role, string title, string message, NotificationType type, string? relatedId = null)
        {
            try
            {
                var userIds = role switch
                {
                    NotificationRole.Admin => await _context.Users.OfType<Admin>().Select(u => u.Id).ToListAsync(),
                    NotificationRole.Instructor => await _context.Users.OfType<Instructor>().Select(u => u.Id).ToListAsync(),
                    NotificationRole.Student => await _context.Users.OfType<Student>().Select(u => u.Id).ToListAsync(),
                    _ => await _context.Users.Select(u => u.Id).ToListAsync()
                };

                var notifications = userIds.Select(userId => new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    CreatedAt = DateTime.UtcNow,
                    RelatedId = relatedId
                }).ToList();

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();

                // Build DTO for real-time
                var dto = new NotificationDTO
                {
                    Id = Guid.NewGuid().ToString(), // Placeholder for bulk notifications
                    Type = type.ToString(),
                    Title = title,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedId = relatedId
                };

                // Send Real-time notification to Group
                var groupName = role.ToString();
                if (role != NotificationRole.All)
                {
                    await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", dto);
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", dto);
                }

                return new ApiResponse("Created", _localizer["Success"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> CreateNotificationForAllAsync(string title, string message, NotificationType type, string? relatedId = null)
        {
            return await CreateNotificationForRoleAsync(NotificationRole.All, title, message, type, relatedId);
        }

        public async Task<ApiResponse> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 10, bool? isRead = null)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);

                var query = _context.Notifications
                    .Where(n => n.UserId == userId);

                if (isRead.HasValue)
                {
                    query = query.Where(n => n.IsRead == isRead.Value);
                }

                var totalCount = await query.CountAsync();

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var notificationDtos = notifications.Select(n => new NotificationDTO
                {
                    Id = n.Id,
                    Type = n.Type.ToString(),
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    RelatedId = n.RelatedId
                }).ToList();

                var pagedResult = new PagedResult<NotificationDTO>
                {
                    Items = notificationDtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };

                return new ApiResponse("Success", _localizer["Success"].Value, pagedResult, true);
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




