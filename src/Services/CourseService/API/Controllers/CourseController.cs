using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Shared.Domain.Entities;

namespace src.Services.CourseService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ITagRepository _tagRepository;

        public CourseController(ICourseRepository courseRepository, ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
            _courseRepository = courseRepository;
        }

        //[Authorize(Roles = "Admin,Instructor")]
        [HttpGet("tags")]
        public async Task<ActionResult<ApiResponse>> GetTags()
        {
            var response = await _tagRepository.GetAllTagsAsync();

            return response.Code switch
            {
                "Success" => Ok(response),
                _ => StatusCode(500, response)
            };
        }   

        [HttpPost("create-tags")]
        // [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse>> CreateTag(CreateTagDTO createTagDTO)
        {
            var response = await _tagRepository.CreateTagAsync(createTagDTO);

            return response.Code switch
            {
                "Success" => Created("", response),
                "Conflict" => Conflict(response),
                "BadRequest" => BadRequest(response),
                _ => StatusCode(500, response)
            };
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("assign-tag")]
        public async Task<ActionResult<ApiResponse>> AssignTagToCourse([FromBody] AssignTagToCourseDTO assignTagToCourseDTO)
        {
            var response = await _tagRepository.AssignTagToCourseAsync(assignTagToCourseDTO);

            return response.Code switch
            {
                "Success" => Created("", response),
                "NotFound" => NotFound(response),
                "BadRequest" => BadRequest(response),
                _ => StatusCode(500, response)
            };
        }
        public async Task<ActionResult<ApiResponse>> GetAllTags()
        {
            var response = await _tagRepository.GetAllTagsAsync();

            return response.Code switch
            {
                "Success" => Ok(response),
                _ => StatusCode(500, response)
            };
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse>> CreateCourse([FromForm] CreateCourseDTO createCourseDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (instructorId == null)
            {
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));
            }
            var response = await _courseRepository.CreateCourseAsync(createCourseDTO, instructorId);

            return response.Code switch
            {
                "Success" => Created("", response),
                "BadRequest" => BadRequest(response),
                _ => StatusCode(500, response)
            };
        }

        [HttpGet("course-detail")]
        public async Task<ActionResult<ApiResponse>> GetCourseDetail([FromQuery] string courseId)
        {
            var response = await _courseRepository.GetCourseDetailAsync(courseId);

            return response.Code switch
            {
                "Success" => Ok(response),
                "NotFound" => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        [HttpGet("comments")]
        public async Task<ActionResult<ApiResponse>> GetComments([FromQuery] string courseId)
        {
            var response = await _courseRepository.GetCourseCommentsAsync(courseId);

            return response.Code switch
            {
                "Success" => Ok(response),
                "NotFound" => NotFound(response),
                _ => StatusCode(500, response)
            };
        }
        [Authorize(Policy = "Student")]
        [HttpGet("recommended-courses")]
        public async Task<ActionResult<ApiResponse>> GetRecommendedCourses()
        {
            var response = await _courseRepository.GetRecommendedCoursesAsync();

            return response.Code switch
            {
                "Success" => Ok(response),
                "NotFound" => NotFound(response),
                _ => StatusCode(500, response)
            };
        }
        [Authorize(Policy = "Student")]
        [HttpGet("student-courses")]
        public async Task<ActionResult<ApiResponse>> GetMyCourses()
        {
            var studentId = User.Claims.FirstOrDefault(c =>
                c.Type == "id")?.Value;

            if (studentId == null)
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));

            var response = await _courseRepository.GetCoursesByStudentIdAsync(studentId);

            return response.Code switch
            {
                "Success" => Ok(response),
                "NotFound" => NotFound(response),
                _ => StatusCode(500, response)
            };
        }
    }
}