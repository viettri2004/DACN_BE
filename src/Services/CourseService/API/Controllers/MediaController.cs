using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.cloudinaryService;
using src.Shared.Domain.Entities;
using System.Security.Claims;
using Shared.Application.Extension;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly CloudinaryService _cloudinaryService;

        public MediaController(CloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        [Authorize]
        [HttpGet("image-signature")]
        public async Task<ActionResult<ApiResponse>> GetImageSignature([FromQuery] string folder = "uploads")
        {
            var response = await _cloudinaryService.GetImageUploadSignatureAsync(folder);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpGet("raw-signature")]
        public async Task<ActionResult<ApiResponse>> GetRawSignature([FromQuery] string folder = "documents")
        {
            var response = await _cloudinaryService.GetRawUploadSignatureAsync(folder);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpGet("video-signature/{lectureId}")]
        public async Task<ActionResult<ApiResponse>> GetVideoSignature(string lectureId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }

            var response = await _cloudinaryService.GetVideoUploadSignatureAsync(lectureId, userId);
            return response.ToActionResult();
        }
    }
}