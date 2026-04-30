using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Resources;
using CourseService.Domain.Enums;

namespace CourseService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ILuceneSearchService _searchService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public CourseController(ICourseRepository courseRepository, ILuceneSearchService searchService, IStringLocalizer<SharedResources> localizer)
        {
            _courseRepository = courseRepository;
            _searchService = searchService;
            _localizer = localizer;
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse>> CreateCourse([FromForm] CreateCourseDTO createCourseDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.CreateCourseAsync(createCourseDTO, instructorId);

            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPut("{courseId}")]
        public async Task<ActionResult<ApiResponse>> UpdateCourse([FromRoute] string courseId, [FromForm] UpdateCourseDTO updateCourseDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.UpdateCourseAsync(courseId, updateCourseDTO, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpGet("instructor-courses")]
        public async Task<ActionResult<ApiResponse>> GetInstructorCourses()
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.GetCoursesByInstructorAsync(instructorId);
            return response.ToActionResult();
        }

        [HttpGet("course-detail/{courseId}")]
        public async Task<ActionResult<ApiResponse>> GetCourseDetail([FromRoute] string courseId)
        {
            var studentId = User.Claims.FirstOrDefault(c =>
                c.Type == "id")?.Value;

            var response = await _courseRepository.GetCourseDetailAsync(courseId, studentId ?? string.Empty);

            return response.ToActionResult();
        }

        [HttpGet("course-comments/{courseId}")]
        public async Task<ActionResult<ApiResponse>> GetComments([FromRoute] string courseId, [FromQuery] CommentType type)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            var response = await _courseRepository.GetCourseCommentsAsync(courseId, userId, type);

            return response.ToActionResult();
        }

        [Authorize]
        [HttpPut("update-comment/{commentId}")]
        public async Task<ActionResult<ApiResponse>> UpdateComment([FromRoute] string commentId, [FromBody] UpdateCommentDTO updateCommentDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.UpdateCommentAsync(commentId, updateCommentDTO, userId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpDelete("delete-comment/{commentId}")]
        public async Task<ActionResult<ApiResponse>> DeleteComment([FromRoute] string commentId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.DeleteCommentAsync(commentId, userId);
            return response.ToActionResult();
        }

        [HttpGet("recommended-courses")]
        public async Task<ActionResult<ApiResponse>> GetRecommendedCourses()
        {
            var response = await _courseRepository.GetRecommendedCoursesAsync();

            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpGet("student-courses")]
        public async Task<ActionResult<ApiResponse>> GetMyCourses()
        {
            var studentId = User.Claims.FirstOrDefault(c =>
                c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));

            var response = await _courseRepository.GetCoursesByStudentIdAsync(studentId);

            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpGet("continue-learning")]
        public async Task<ActionResult<ApiResponse>> GetContinueLearning()
        {
            var studentId = User.Claims.FirstOrDefault(c =>
                c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));

            var response = await _courseRepository.GetContinueLearningCoursesAsync(studentId);

            return response.ToActionResult();
        }
        [Authorize]
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse>> SearchCourses([FromQuery] CourseSearchDTO queryParams)
        {
            // Support both SelectedTags and TagId[] / tagIds format for compatibility
            if (queryParams.SelectedTags == null || !queryParams.SelectedTags.Any())
            {
                if (Request.Query.TryGetValue("TagId[]", out var tagIds))
                {
                    queryParams.SelectedTags = tagIds.ToList()!;
                }
                else if (Request.Query.TryGetValue("tagIds", out var tIds))
                {
                    queryParams.SelectedTags = tIds.ToList()!;
                }
                else if (Request.Query.TryGetValue("SelectedTags[]", out var sTags))
                {
                    queryParams.SelectedTags = sTags.ToList()!;
                }
            }

            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _searchService.SearchCoursesAsync(queryParams, studentId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpGet("search-preview")]
        public async Task<ActionResult<ApiResponse>> SearchCoursesPreview([FromQuery] string searchTerm)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            var response = await _searchService.SearchCoursesPreviewAsync(searchTerm, studentId ?? string.Empty);
            return response.ToActionResult();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("re-index")]
        public async Task<IActionResult> ReIndexAllCourses()
        {
            await _searchService.IndexAllCoursesAsync();
            return Ok(new { message = "Re-indexing process started." });
        }


        [Authorize]
        [HttpGet("course-content/{courseId}")]
        public async Task<ActionResult<ApiResponse>> GetCourseContent([FromRoute] string courseId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _courseRepository.GetCourseContentAsync(courseId, userId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpDelete("{courseId}")]
        public async Task<ActionResult<ApiResponse>> DeleteCourse([FromRoute] string courseId)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.DeleteCourseAsync(courseId, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("request-publish/{courseId}")]
        public async Task<ActionResult<ApiResponse>> RequestPublishCourse([FromRoute] string courseId)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.CreateCourseRequestAsync(courseId, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Admin")]
        [HttpGet("pending-requests")]
        public async Task<ActionResult<ApiResponse>> GetPendingRequests()
        {
            var response = await _courseRepository.GetPendingCourseRequestsAsync();
            return response.ToActionResult();
        }

        [Authorize(Policy = "Admin")]
        [HttpPost("approve-request/{requestId}")]
        public async Task<ActionResult<ApiResponse>> ApproveRequest([FromRoute] string requestId, [FromBody] ResponseRequestDTO responseRequestDTO)
        {
            var response = await _courseRepository.ApproveCourseRequestAsync(requestId, responseRequestDTO);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Admin")]
        [HttpPost("reject-request/{requestId}")]
        public async Task<ActionResult<ApiResponse>> RejectRequest([FromRoute] string requestId, [FromBody] ResponseRequestDTO responseRequestDTO)
        {
            var response = await _courseRepository.RejectCourseRequestAsync(requestId, responseRequestDTO);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Admin")]
        [HttpGet("admin/courses")]
        public async Task<ActionResult<ApiResponse>> GetAllCoursesForAdmin()
        {
            var response = await _courseRepository.GetAllCoursesForAdminAsync();
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpPost("add-comment")]
        public async Task<ActionResult<ApiResponse>> AddComment([FromBody] AddCommentDTO addCommentDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.AddCommentAsync(addCommentDTO, userId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("reply-comment")]
        public async Task<ActionResult<ApiResponse>> ReplyComment([FromBody] AddReplyCommentDTO replyDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.ReplyToCommentAsync(replyDTO, userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpGet("course-qas/{courseId}")]
        public async Task<ActionResult<ApiResponse>> GetCourseQAs([FromRoute] string courseId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.GetCourseQAsAsync(courseId, userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpPost("add-question")]
        public async Task<ActionResult<ApiResponse>> CreateThread([FromBody] CreateThreadDTO createThreadDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.CreateQAThreadAsync(createThreadDTO, userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpPost("reply-qa")]
        public async Task<ActionResult<ApiResponse>> AddMessage([FromBody] AddMessageDTO addMessageDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.AddMessageToThreadAsync(addMessageDTO, userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpPut("update-thread/{threadId}")]
        public async Task<ActionResult<ApiResponse>> UpdateThread([FromRoute] string threadId, [FromBody] UpdateThreadDTO updateThreadDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.UpdateQAThreadAsync(threadId, updateThreadDTO, userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpPut("update-message/{messageId}")]
        public async Task<ActionResult<ApiResponse>> UpdateMessage([FromRoute] string messageId, [FromBody] UpdateMessageDTO updateMessageDTO)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.UpdateQAMessageAsync(messageId, updateMessageDTO, userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpDelete("delete-thread/{threadId}")]
        public async Task<ActionResult<ApiResponse>> DeleteThread([FromRoute] string threadId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.DeleteQAThreadAsync(threadId, userId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpDelete("delete-message/{messageId}")]
        public async Task<ActionResult<ApiResponse>> DeleteMessage([FromRoute] string messageId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.DeleteQAMessageAsync(messageId, userId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpPost("mark-completed")]
        public async Task<ActionResult<ApiResponse>> MarkItemCompleted([FromBody] MarkItemCompletedDTO dto)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _courseRepository.MarkItemCompletedAsync(dto, studentId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpPost("unmark-completed")]
        public async Task<ActionResult<ApiResponse>> UnmarkItemCompleted([FromBody] MarkItemCompletedDTO dto)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _courseRepository.UnmarkItemCompletedAsync(dto, studentId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpPost("wishlist/add/{courseId}")]
        public async Task<ActionResult<ApiResponse>> AddToWishlist([FromRoute] string courseId)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.AddToWishlistAsync(courseId, studentId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpDelete("wishlist/remove/{courseId}")]
        public async Task<ActionResult<ApiResponse>> RemoveFromWishlist([FromRoute] string courseId)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.RemoveFromWishlistAsync(courseId, studentId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpGet("wishlist")]
        public async Task<ActionResult<ApiResponse>> GetMyWishlist()
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.GetStudentWishlistAsync(studentId);
            return response.ToActionResult();
        }
    }
}
