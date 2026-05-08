using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
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
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using src.Shared.Domain.Entities;

namespace NotificationService.Application.Interfaces
{
    public interface INotificationRepository
    {
        Task<ApiResponse> CreateNotificationAsync(Notification notification);
        Task<ApiResponse> CreateNotificationForRoleAsync(NotificationRole role, string title, string message, NotificationType type, string? relatedId = null);
        Task<ApiResponse> CreateNotificationForAllAsync(string title, string message, NotificationType type, string? relatedId = null);
        Task<ApiResponse> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 10, bool? isRead = null);
        Task<ApiResponse> MarkAsReadAsync(string notificationId);
        Task<ApiResponse> MarkAllAsReadAsync(string userId);
    }
}



