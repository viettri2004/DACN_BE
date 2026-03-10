using System.Collections.Generic;
using System.Threading.Tasks;
using src.Shared.Domain.Entities;

namespace Shared.Application.Interfaces
{
    public interface INotificationRepository
    {
        Task CreateNotificationAsync(Notification notification);
        Task<List<Notification>> GetUserNotificationsAsync(string userId);
        Task<List<Notification>> GetAdminNotificationsAsync();
        Task MarkAsReadAsync(string notificationId);
        Task MarkAllAsReadAsync(string userId);
    }
}
