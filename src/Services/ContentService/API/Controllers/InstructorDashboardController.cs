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
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace ContentService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Instructor")]
    public class InstructorDashboardController : ControllerBase
    {
        private readonly IInstructorService _instructorService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public InstructorDashboardController(IInstructorService instructorService, IStringLocalizer<SharedResources> localizer)
        {
            _instructorService = instructorService;
            _localizer = localizer;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse>> GetStats()
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _instructorService.GetDashboardAsync(instructorId);
            return response.ToActionResult();
        }

        [HttpGet("activities")]
        public async Task<ActionResult<ApiResponse>> GetActivities([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _instructorService.GetActivitiesAsync(instructorId, page, pageSize);
            return response.ToActionResult();
        }

        [HttpGet("unread-threads")]
        public async Task<ActionResult<ApiResponse>> GetUnreadThreads()
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _instructorService.GetUnreadThreadsAsync(instructorId);
            return response.ToActionResult();
        }
    }
}


