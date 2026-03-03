using System.Linq;
using System.Threading.Tasks;
using LectureService.Application.DTOs;
using LectureService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;

namespace LectureService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly IQuizRepository _quizRepository;

        public QuizController(IQuizRepository quizRepository)
        {
            _quizRepository = quizRepository;
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse>> CreateQuiz([FromForm] CreateQuizDTO createQuizDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }
            var response = await _quizRepository.CreateQuizAsync(createQuizDTO, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPatch("{quizId}")]
        public async Task<ActionResult<ApiResponse>> UpdateQuiz([FromRoute] string quizId, [FromForm] UpdateQuizDTO updateQuizDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }
            var response = await _quizRepository.UpdateQuizAsync(quizId, updateQuizDTO, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpDelete("{quizId}")]
        public async Task<ActionResult<ApiResponse>> DeleteQuiz([FromRoute] string quizId)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }
            var response = await _quizRepository.DeleteQuizAsync(quizId, instructorId);
            return response.ToActionResult();
        }

        [HttpGet("{quizId}")]
        public async Task<ActionResult<ApiResponse>> GetQuiz([FromRoute] string quizId)
        {
            var response = await _quizRepository.GetQuizByIdAsync(quizId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpPost("{quizId}/attempt")]
        public async Task<ActionResult<ApiResponse>> StartQuiz([FromRoute] string quizId)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            } 

            var response = await _quizRepository.StartQuizAttemptAsync(quizId, studentId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpPost("submit")]
        public async Task<ActionResult<ApiResponse>> SubmitQuiz([FromBody] QuizSubmissionDTO submissionDTO)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }

            var response = await _quizRepository.SubmitQuizAttemptAsync(submissionDTO, studentId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpGet("attempt/{attemptId}/result")]
        public async Task<ActionResult<ApiResponse>> GetQuizResult([FromRoute] string attemptId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));

            var response = await _quizRepository.GetQuizResultAsync(attemptId, userId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpGet("{quizId}/attempts")]
        public async Task<ActionResult<ApiResponse>> GetStudentAttempts([FromRoute] string quizId)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));

            var response = await _quizRepository.GetStudentQuizAttemptsAsync(quizId, studentId);
            return response.ToActionResult();
        }
    }
}