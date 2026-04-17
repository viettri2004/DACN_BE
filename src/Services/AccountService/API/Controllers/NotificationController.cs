using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Resources;

namespace AccountService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public NotificationController(INotificationRepository notificationRepository, IStringLocalizer<SharedResources> localizer)
        {
            _notificationRepository = notificationRepository;
            _localizer = localizer;
        }

        [Authorize]
        [HttpGet("my-notifications")]
        public async Task<ActionResult<ApiResponse>> GetMyNotifications()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _notificationRepository.GetUserNotificationsAsync(userId);
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
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _notificationRepository.MarkAllAsReadAsync(userId);
            return response.ToActionResult();
        }
    }
}
