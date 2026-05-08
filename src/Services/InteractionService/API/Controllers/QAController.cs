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
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using InteractionService.Application.DTOs;
using InteractionService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace InteractionService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QAController : ControllerBase
    {
        private readonly IQAThreadService _qaService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public QAController(IQAThreadService qaService, IStringLocalizer<SharedResources> localizer)
        {
            _qaService = qaService;
            _localizer = localizer;
        }

        [HttpGet("threads/{courseId}")]
        public async Task<ActionResult<ApiResponse>> GetCourseQAThreads([FromRoute] string courseId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string filter = "all")
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _qaService.GetCourseQAThreadsAsync(courseId, userId, pageNumber, pageSize, filter);
            return response.ToActionResult();
        }

        [HttpGet("thread/{threadId}/messages")]
        public async Task<ActionResult<ApiResponse>> GetThreadMessages([FromRoute] string threadId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _qaService.GetThreadMessagesAsync(threadId, userId, pageNumber, pageSize);
            return response.ToActionResult();
        }

        [HttpPost("thread/create")]
        public async Task<ActionResult<ApiResponse>> CreateThread([FromBody] CreateThreadDTO createThreadDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _qaService.CreateQAThreadAsync(createThreadDTO, userId);
            return response.ToActionResult();
        }

        [HttpPost("message/reply")]
        public async Task<ActionResult<ApiResponse>> AddMessage([FromBody] AddMessageDTO addMessageDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _qaService.AddMessageToThreadAsync(addMessageDTO, userId);
            return response.ToActionResult();
        }

        [HttpPut("thread/{threadId}")]
        public async Task<ActionResult<ApiResponse>> UpdateThread([FromRoute] string threadId, [FromBody] UpdateThreadDTO updateThreadDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _qaService.UpdateQAThreadAsync(threadId, updateThreadDTO, userId);
            return response.ToActionResult();
        }

        [HttpPut("message/{messageId}")]
        public async Task<ActionResult<ApiResponse>> UpdateMessage([FromRoute] string messageId, [FromBody] UpdateMessageDTO updateMessageDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _qaService.UpdateQAMessageAsync(messageId, updateMessageDTO, userId);
            return response.ToActionResult();
        }

        [HttpDelete("thread/{threadId}")]
        public async Task<ActionResult<ApiResponse>> DeleteThread([FromRoute] string threadId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _qaService.DeleteQAThreadAsync(threadId, userId);
            return response.ToActionResult();
        }

        [HttpDelete("message/{messageId}")]
        public async Task<ActionResult<ApiResponse>> DeleteMessage([FromRoute] string messageId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _qaService.DeleteQAMessageAsync(messageId, userId);
            return response.ToActionResult();
        }
    }
}


