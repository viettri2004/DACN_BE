using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Domain.Enums;
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
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public CommentController(ICommentService commentService, IStringLocalizer<SharedResources> localizer)
        {
            _commentService = commentService;
            _localizer = localizer;
        }

        [HttpGet("course/{courseId}")]
        public async Task<ActionResult<ApiResponse>> GetComments([FromRoute] string courseId, [FromQuery] CommentType type, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] int? rating = null)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            var response = await _commentService.GetCourseCommentsAsync(courseId, userId, type, pageNumber, pageSize, rating);

            return response.ToActionResult();
        }

        [Authorize]
        [HttpPost("add")]
        public async Task<ActionResult<ApiResponse>> AddComment([FromBody] AddCommentDTO addCommentDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _commentService.AddCommentAsync(addCommentDTO, userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpPut("{commentId}")]
        public async Task<ActionResult<ApiResponse>> UpdateComment([FromRoute] string commentId, [FromBody] UpdateCommentDTO updateCommentDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _commentService.UpdateCommentAsync(commentId, updateCommentDTO, userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpDelete("{commentId}")]
        public async Task<ActionResult<ApiResponse>> DeleteComment([FromRoute] string commentId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _commentService.DeleteCommentAsync(commentId, userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpPost("reply")]
        public async Task<ActionResult<ApiResponse>> ReplyComment([FromBody] AddReplyCommentDTO replyDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _commentService.ReplyToCommentAsync(replyDTO, userId);
            return response.ToActionResult();
        }
    }
}
