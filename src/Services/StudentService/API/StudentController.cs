using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Shared.Domain.Entities;
using StudentService.Application.Interfaces;

namespace src.Services.StudentService.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentRepository _studentRepository;
        public StudentController(IStudentRepository studentRepository)
        {
            _studentRepository = studentRepository;
        }
        [Authorize(Policy = "Student")]
        [HttpGet("recommended-courses")]
        public async Task<ActionResult<ApiResponse>> GetRecommendedCourses()
        {
            var response = await _studentRepository.GetRecommendedCoursesAsync();

            return response.Code switch
            {
                "Success" => Ok(response),
                "NotFound" => NotFound(response),
                _ => StatusCode(500, response)
            };
        }
        [Authorize(Policy = "Student")]
        [HttpGet("my-courses")]
        public async Task<ActionResult<ApiResponse>> GetMyCourses()
        {
            var studentId = User.Claims.FirstOrDefault(c =>
                c.Type == "id")?.Value;

            if (studentId == null)
                return Unauthorized(new ApiResponse("Error", "Unauthorized", null, false));

            var response = await _studentRepository.GetMyCoursesAsync(studentId);

            return response.Code switch
            {
                "Success" => Ok(response),
                "NotFound" => NotFound(response),
                _ => StatusCode(500, response)
            };
        }
    }
}