using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;

namespace AccountService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationController(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        [Authorize]
        [HttpGet("my-notifications")]
        public async Task<ActionResult<ApiResponse>> GetMyNotifications()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            var roles = User.Claims
                .Where(c => c.Type == "role")
                .Select(c => c.Value)
                .ToList();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }

            var response = await _notificationRepository.GetUserNotificationsAsync(userId, roles);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpPost("mark-as-read/{notificationId}")]
        public async Task<ActionResult<ApiResponse>> MarkAsRead(string notificationId)
        {
            var response = await _notificationRepository.MarkAsReadAsync(notificationId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpPost("mark-all-as-read")]
        public async Task<ActionResult<ApiResponse>> MarkAllAsRead()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }

            var response = await _notificationRepository.MarkAllAsReadAsync(userId);
            return response.ToActionResult();
        }
    }
}
