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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.cloudinaryService;
using src.Shared.Domain.Entities;
using System.Security.Claims;
using Shared.Application.Extension;

namespace ContentService.API.Controllers
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


