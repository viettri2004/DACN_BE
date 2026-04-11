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

        public WebhookController(CloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("cloudinary")]
        public async Task<IActionResult> HandleCloudinaryEvent([FromBody] JsonElement payload)
        {
            var isSuccess = await _cloudinaryService.ProcessCloudinaryWebhookAsync(payload);
            
            if (isSuccess) return Ok();
            
            return BadRequest();
        }
    }
}
