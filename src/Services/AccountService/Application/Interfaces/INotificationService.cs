using System.Threading.Tasks;
using AccountService.Domain.Enums;
using Entities;
using src.Shared.Domain.Entities;

namespace AccountService.Application.Interfaces
{
    public interface INotificationService
    {
        Task<ApiResponse> CreateNotificationAsync(Notification notification);
        Task<ApiResponse> CreateNotificationForRoleAsync(NotificationRole role, string title, string message, NotificationType type);
        Task<ApiResponse> CreateNotificationForAllAsync(string title, string message, NotificationType type);
        Task<ApiResponse> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 10, bool? isRead = null);
        Task<ApiResponse> MarkAsReadAsync(string notificationId);
        Task<ApiResponse> MarkAllAsReadAsync(string userId);
    }
}
