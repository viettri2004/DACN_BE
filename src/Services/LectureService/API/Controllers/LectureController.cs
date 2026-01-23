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
        [HttpPost("add-video/{lectureId}")]
        public async Task<ActionResult<ApiResponse>> AddVideo([FromRoute] string lectureId, IFormFile videoFile)
        {
            var response = await _lectureRepository.AddVideoToLectureAsync(lectureId, videoFile);
            return response.ToActionResult();
        }
    }
}