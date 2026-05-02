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
        private readonly ICourseService _courseService;
        private readonly ILuceneSearchService _searchService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public CourseController(ICourseService courseService, ILuceneSearchService searchService, IStringLocalizer<SharedResources> localizer)
        {
            _courseService = courseService;
            _searchService = searchService;
            _localizer = localizer;
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse>> CreateCourse([FromBody] CreateCourseDTO createCourseDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseService.CreateCourseAsync(createCourseDTO, instructorId);

            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPut("{courseId}")]
        public async Task<ActionResult<ApiResponse>> UpdateCourse([FromRoute] string courseId, [FromBody] UpdateCourseDTO updateCourseDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseService.UpdateCourseAsync(courseId, updateCourseDTO, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpGet("instructor-courses")]
        public async Task<ActionResult<ApiResponse>> GetInstructorCourses([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseService.GetCoursesByInstructorAsync(instructorId, pageNumber, pageSize);
            return response.ToActionResult();
        }

        [HttpGet("course-detail/{courseId}")]
        public async Task<ActionResult<ApiResponse>> GetCourseDetail([FromRoute] string courseId)
        {
            var studentId = User.Claims.FirstOrDefault(c =>
                c.Type == "id")?.Value;

            var response = await _courseService.GetCourseDetailAsync(courseId, studentId ?? string.Empty);

            return response.ToActionResult();
        }

        [HttpGet("recommended-courses")]
        public async Task<ActionResult<ApiResponse>> GetRecommendedCourses([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            var response = await _courseService.GetRecommendedCoursesAsync(userId, pageNumber, pageSize);

            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
        [HttpGet("student-courses")]
        public async Task<ActionResult<ApiResponse>> GetMyCourses([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var studentId = User.Claims.FirstOrDefault(c =>
                c.Type == "id")?.Value;

            if (string.IsNullOrEmpty(studentId))
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));

            var response = await _courseService.GetCoursesByStudentIdAsync(studentId, pageNumber, pageSize);

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

            var response = await _courseService.GetContinueLearningCoursesAsync(studentId);

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

            var response = await _courseService.GetCourseContentAsync(courseId, userId);
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
            var response = await _courseService.DeleteCourseAsync(courseId, instructorId);
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
            var response = await _courseService.CreateCourseRequestAsync(courseId, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Admin")]
        [HttpGet("pending-requests")]
        public async Task<ActionResult<ApiResponse>> GetPendingRequests([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var response = await _courseService.GetPendingCourseRequestsAsync(pageNumber, pageSize);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Admin")]
        [HttpPost("approve-request/{requestId}")]
        public async Task<ActionResult<ApiResponse>> ApproveRequest([FromRoute] string requestId, [FromBody] ResponseRequestDTO responseRequestDTO)
        {
            var response = await _courseService.ApproveCourseRequestAsync(requestId, responseRequestDTO);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Admin")]
        [HttpPost("reject-request/{requestId}")]
        public async Task<ActionResult<ApiResponse>> RejectRequest([FromRoute] string requestId, [FromBody] ResponseRequestDTO responseRequestDTO)
        {
            var response = await _courseService.RejectCourseRequestAsync(requestId, responseRequestDTO);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Admin")]
        [HttpGet("admin/courses")]
        public async Task<ActionResult<ApiResponse>> GetAllCoursesForAdmin([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var response = await _courseService.GetAllCoursesForAdminAsync(pageNumber, pageSize);
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

            var response = await _courseService.MarkItemCompletedAsync(dto, studentId);
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

            var response = await _courseService.UnmarkItemCompletedAsync(dto, studentId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpGet("instructor-dashboard")]
        public async Task<ActionResult<ApiResponse>> GetInstructorDashboard()
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseService.GetInstructorDashboardAsync(instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpGet("instructor-activities")]
        public async Task<ActionResult<ApiResponse>> GetInstructorActivities([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseService.GetInstructorActivitiesAsync(instructorId, page, pageSize);
            return response.ToActionResult();
        }
        
        [Authorize(Policy = "Instructor")]
        [HttpGet("instructor-unread-threads")]
        public async Task<ActionResult<ApiResponse>> GetInstructorUnreadThreads()
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _courseService.GetInstructorUnreadThreadsAsync(instructorId);
            return response.ToActionResult();
        }
    }
}
