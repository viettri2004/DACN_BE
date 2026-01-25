using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LectureService.Application.DTOs;
using LectureService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace LectureService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LectureController : ControllerBase
    {
        private readonly ILectureRepository _lectureRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public LectureController(ILectureRepository lectureRepository, IStringLocalizer<SharedResources> localizer)
        {
            _lectureRepository = lectureRepository;
            _localizer = localizer;
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("create-lecture")]
        public async Task<ActionResult<ApiResponse>> CreateLecture([FromBody] CreateLectureDTO createLectureDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.CreateLectureAsync(createLectureDTO, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPut("update-lecture/{lectureId}")]
        public async Task<ActionResult<ApiResponse>> UpdateLecture([FromRoute] string lectureId, [FromBody] UpdateLectureDTO updateLectureDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.UpdateLectureAsync(lectureId, updateLectureDTO, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpDelete("delete-lecture/{lectureId}")]
        public async Task<ActionResult<ApiResponse>> DeleteLecture([FromRoute] string lectureId)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.DeleteLectureAsync(lectureId, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("add-video/{lectureId}")]
        public async Task<ActionResult<ApiResponse>> AddVideo([FromRoute] string lectureId, IFormFile videoFile)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.AddVideoToLectureAsync(lectureId, videoFile, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPut("update-video/{videoId}")]
        public async Task<ActionResult<ApiResponse>> UpdateVideo([FromRoute] string videoId, [FromForm] UpdateLectureVideoDTO updateLectureVideoDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.UpdateVideoAsync(videoId, updateLectureVideoDTO.Name, updateLectureVideoDTO.VideoFile, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpDelete("delete-video/{videoId}")]
        public async Task<ActionResult<ApiResponse>> DeleteVideo([FromRoute] string videoId)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.DeleteVideoAsync(videoId, instructorId);
            return response.ToActionResult();
        }

        [Authorize]
        [HttpGet("get-video/{videoId}")]
        public async Task<ActionResult<ApiResponse>> GetVideoById([FromRoute] string videoId)
        {
            var response = await _lectureRepository.GetVideoByIdAsync(videoId);
            return response.ToActionResult();
        }

        
    }
}