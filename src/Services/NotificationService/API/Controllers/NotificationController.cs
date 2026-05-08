using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using OrderingService.Domain.Entities;
using IdentityService.Application.DTOs;
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
using System.Linq;
using System.Threading.Tasks;
using IdentityService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Resources;

namespace NotificationService.API.Controllers
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
        public async Task<ActionResult<ApiResponse>> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool? isRead = null)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _notificationRepository.GetUserNotificationsAsync(userId, page, pageSize, isRead);
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



