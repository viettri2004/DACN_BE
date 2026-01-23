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

namespace CourseService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ILuceneSearchService _searchService;

        public CourseController(ICourseRepository courseRepository, ILuceneSearchService searchService)
        {
            _courseRepository = courseRepository;
            _searchService = searchService;
        }

        [Authorize]
        [HttpGet("filtered-courses")]
        public async Task<ActionResult<ApiResponse>> GetAllCourses([FromQuery] CourseQueryParameters queryParams)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
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
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }
            var response = await _courseRepository.CreateCourseAsync(createCourseDTO, instructorId);

            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpGet("instructor-courses")]
        public async Task<ActionResult<ApiResponse>> GetInstructorCourses()
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }
            var response = await _courseRepository.GetCoursesByInstructorAsync(instructorId);
            return response.ToActionResult();
        }

        [HttpGet("course-detail/{courseId}")]
        public async Task<ActionResult<ApiResponse>> GetCourseDetail([FromRoute] string courseId)
        {
            var studentId = User.Claims.FirstOrDefault(c =>
                c.Type == "id")?.Value;

            var response = await _courseRepository.GetCourseDetailAsync(courseId, studentId);

            return response.ToActionResult();
        }

        [HttpGet("course-comments/{courseId}")]
        public async Task<ActionResult<ApiResponse>> GetComments([FromRoute] string courseId)
        {
            var response = await _courseRepository.GetCourseCommentsAsync(courseId);

            return response.ToActionResult();
        }

        [Authorize(Policy = "Student")]
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
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));

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
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
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

        
        [HttpGet("course-content/{courseId}")]
        public async Task<ActionResult<ApiResponse>> GetCourseContent([FromRoute] string courseId)
        {
            var response = await _courseRepository.GetCourseContentAsync(courseId);
            return response.ToActionResult();
        }
    }
}
