using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace CourseService.API.Controllers
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
