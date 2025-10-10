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

        public CourseController(ICourseRepository courseRepository)
        {
            _courseRepository = courseRepository;
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
    }
}