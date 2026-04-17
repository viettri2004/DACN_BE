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

        [Authorize]
        [HttpGet("filtered-courses")]
        public async Task<ActionResult<ApiResponse>> GetAllCourses([FromQuery] CourseQueryParameters queryParams)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseRepository.GetCoursesAsync(queryParams, studentId);
            return response.ToActionResult();
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
        public async Task<ActionResult<ApiResponse>> GetComments([FromRoute] string courseId)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            var response = await _courseRepository.GetCourseCommentsAsync(courseId, userId);

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

        [Authorize]
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse>> SearchCourses([FromQuery] CourseSearchDTO queryParams)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _searchService.SearchCoursesAsync(queryParams, studentId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Admin")]
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

        [Authorize(Policy = "Student")]
        [HttpPost("mark-completed/{lectureId}")]
        public async Task<ActionResult<ApiResponse>> MarkLectureCompleted([FromRoute] string lectureId)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _courseRepository.MarkLectureCompletedAsync(lectureId, studentId);
            return response.ToActionResult();
        }
    }
}
