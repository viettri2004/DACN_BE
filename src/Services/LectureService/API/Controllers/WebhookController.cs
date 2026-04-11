using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.cloudinaryService;
using System.Text.Json;
using System.Threading.Tasks;

namespace src.Services.LectureService.API.Controllers
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
