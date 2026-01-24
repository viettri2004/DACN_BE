using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LectureService.Application.DTOs;
using LectureService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;

namespace LectureService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LectureController : ControllerBase
    {
        private readonly ILectureRepository _lectureRepository;

        public LectureController(ILectureRepository lectureRepository)
        {
            _lectureRepository = lectureRepository;
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("create-lecture")]
        public async Task<ActionResult<ApiResponse>> CreateLecture([FromBody] CreateLectureDTO createLectureDTO)
        {
            var response = await _lectureRepository.CreateLectureAsync(createLectureDTO);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPut("update-lecture/{lectureId}")]
        public async Task<ActionResult<ApiResponse>> UpdateLecture([FromRoute] string lectureId, [FromBody] UpdateLectureDTO updateLectureDTO)
        {
            var response = await _lectureRepository.UpdateLectureAsync(lectureId, updateLectureDTO);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpDelete("delete-lecture/{lectureId}")]
        public async Task<ActionResult<ApiResponse>> DeleteLecture([FromRoute] string lectureId)
        {
            var response = await _lectureRepository.DeleteLectureAsync(lectureId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("add-video/{lectureId}")]
        public async Task<ActionResult<ApiResponse>> AddVideo([FromRoute] string lectureId, IFormFile videoFile)
        {
            var response = await _lectureRepository.AddVideoToLectureAsync(lectureId, videoFile);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPut("update-video/{videoId}")]
        public async Task<ActionResult<ApiResponse>> UpdateVideo([FromRoute] string videoId, [FromForm] string name, [FromForm] IFormFile? videoFile)
        {
            var response = await _lectureRepository.UpdateVideoAsync(videoId, name, videoFile);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpDelete("delete-video/{videoId}")]
        public async Task<ActionResult<ApiResponse>> DeleteVideo([FromRoute] string videoId)
        {
            var response = await _lectureRepository.DeleteVideoAsync(videoId);
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