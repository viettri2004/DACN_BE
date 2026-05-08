using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
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
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.cloudinaryService;
using System.Text.Json;
using System.Threading.Tasks;

namespace ContentService.API.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhookController : ControllerBase
    {
        private readonly CloudinaryService _cloudinaryService;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(CloudinaryService cloudinaryService, ILogger<WebhookController> logger)
        {
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        [HttpPost("cloudinary")]
        public async Task<IActionResult> CloudinaryWebhook()
        {
            try
            {
                using var reader = new System.IO.StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(rawBody))
                {
                    _logger.LogWarning("Webhook nhận được body rỗng.");
                    return Ok();
                }

                var payload = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(rawBody);

                if (payload.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    await _cloudinaryService.ProcessCloudinaryWebhookAsync(payload);
                }
                else
                {
                    _logger.LogWarning("Webhook JSON không phải là Object: {ValueKind}", payload.ValueKind);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi parse cục JSON của Cloudinary!");
            }

            return Ok();
        }
    }
}


