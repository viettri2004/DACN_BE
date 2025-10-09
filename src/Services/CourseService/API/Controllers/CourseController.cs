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
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
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
    }
}