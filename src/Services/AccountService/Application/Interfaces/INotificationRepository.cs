using System.Collections.Generic;
using System.Threading.Tasks;
using Entities;
using src.Shared.Domain.Entities;

namespace AccountService.Application.Interfaces
{
    public interface INotificationRepository
    {
        Task<ApiResponse> CreateNotificationAsync(Notification notification);
        Task<ApiResponse> GetUserNotificationsAsync(string userId, List<string> roles);
        Task<ApiResponse> MarkAsReadAsync(string notificationId);
        Task<ApiResponse> MarkAllAsReadAsync(string userId);
    }
}
