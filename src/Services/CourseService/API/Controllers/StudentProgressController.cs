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
