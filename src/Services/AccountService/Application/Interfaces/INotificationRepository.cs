using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;
using src.Shared.Domain.Entities;
using AccountService.Domain.Enums;

namespace AccountService.Application.Interfaces
{
    public interface INotificationRepository
    {
        Task<ApiResponse> CreateNotificationAsync(Notification notification);
        Task<ApiResponse> CreateNotificationForRoleAsync(NotificationRole role, string title, string message, NotificationType type);
        Task<ApiResponse> CreateNotificationForAllAsync(string title, string message, NotificationType type);
        Task<ApiResponse> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 10);
        Task<ApiResponse> MarkAsReadAsync(string notificationId);
        Task<ApiResponse> MarkAllAsReadAsync(string userId);
    }
}
