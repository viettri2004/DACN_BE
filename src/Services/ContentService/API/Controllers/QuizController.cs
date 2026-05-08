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
using System.Threading.Tasks;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Resources;

namespace ContentService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public QuizController(IQuizService quizService, IStringLocalizer<SharedResources> localizer)
        {
            _quizService = quizService;
            _localizer = localizer;
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse>> CreateQuiz([FromBody] CreateQuizDTO createQuizDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _quizService.CreateQuizAsync(createQuizDTO, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPatch("{quizId}")]
        public async Task<ActionResult<ApiResponse>> UpdateQuiz([FromRoute] string quizId, [FromBody] UpdateQuizDTO updateQuizDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _quizService.UpdateQuizAsync(quizId, updateQuizDTO, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpDelete("{quizId}")]
        public async Task<ActionResult<ApiResponse>> DeleteQuiz([FromRoute] string quizId)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _quizService.DeleteQuizAsync(quizId, instructorId);
            return response.ToActionResult();
        }

        [HttpGet("{quizId}")]
        public async Task<ActionResult<ApiResponse>> GetQuiz([FromRoute] string quizId)
        {
            var response = await _quizService.GetQuizByIdAsync(quizId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpPost("{quizId}/attempt")]
        public async Task<ActionResult<ApiResponse>> StartQuiz([FromRoute] string quizId)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            } 

            var response = await _quizService.StartQuizAttemptAsync(quizId, studentId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpPost("submit")]
        public async Task<ActionResult<ApiResponse>> SubmitQuiz([FromBody] QuizSubmissionDTO submissionDTO)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _quizService.SubmitQuizAttemptAsync(submissionDTO, studentId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpGet("attempt/{attemptId}/result")]
        public async Task<ActionResult<ApiResponse>> GetQuizResult([FromRoute] string attemptId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));

            var response = await _quizService.GetQuizResultAsync(attemptId, userId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpGet("{quizId}/attempts")]
        public async Task<ActionResult<ApiResponse>> GetStudentAttempts([FromRoute] string quizId)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));

            var response = await _quizService.GetStudentQuizAttemptsAsync(quizId, studentId);
            return response.ToActionResult();
        }
    }
}


