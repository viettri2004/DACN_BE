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
using LearningService.Domain.Entities;
using InteractionService.Application.DTOs;
using InteractionService.Application.Interfaces;
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LearningService.Application.DTOs;
using LearningService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace LearningService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Student")]
    public class StudentProgressController : ControllerBase
    {
        private readonly IStudentProgressService _progressService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public StudentProgressController(IStudentProgressService progressService, IStringLocalizer<SharedResources> localizer)
        {
            _progressService = progressService;
            _localizer = localizer;
        }

        [HttpPost("mark-completed")]
        public async Task<ActionResult<ApiResponse>> MarkItemCompleted([FromBody] MarkItemCompletedDTO dto)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _progressService.MarkItemCompletedAsync(dto, studentId);
            return response.ToActionResult();
        }

        [HttpPost("unmark-completed")]
        public async Task<ActionResult<ApiResponse>> UnmarkItemCompleted([FromBody] MarkItemCompletedDTO dto)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _progressService.UnmarkItemCompletedAsync(dto, studentId);
            return response.ToActionResult();
        }

        [HttpGet("continue-learning")]
        public async Task<ActionResult<ApiResponse>> GetContinueLearning()
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _progressService.GetContinueLearningCoursesAsync(studentId);
            return response.ToActionResult();
        }
    }
}


