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

namespace src.Services.CourseService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseRepository _courseRepository;

        public CourseController(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;
        }

        [Authorize]
        [HttpGet("filtered-courses")]
        public async Task<ActionResult<ApiResponse>> GetAllCourses([FromQuery] CourseQueryParameters queryParams)
        {
            var response = await _courseRepository.GetCoursesAsync(queryParams);
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

        [HttpGet("course-detail/{courseId}")]
        public async Task<ActionResult<ApiResponse>> GetCourseDetail([FromRoute] string courseId)
        {
            var response = await _courseRepository.GetCourseDetailAsync(courseId);

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
    }
}