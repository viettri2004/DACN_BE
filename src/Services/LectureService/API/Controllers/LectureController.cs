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
        [HttpPatch("update-orders")]
        public async Task<ActionResult<ApiResponse>> UpdateLectureOrders([FromBody] List<UpdateOrderDTO> lectureOrders)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.UpdateLectureOrdersAsync(lectureOrders, instructorId);
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

        [Authorize]
        [HttpGet("get-video/{videoId}")]
        public async Task<ActionResult<ApiResponse>> GetVideoById([FromRoute] string videoId)
        {
            var response = await _lectureRepository.GetVideoByIdAsync(videoId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpGet("video-upload-signature/{lectureId}")]
        public async Task<ActionResult<ApiResponse>> GetVideoUploadSignature([FromRoute] string lectureId)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }

            var response = await _lectureRepository.GetVideoUploadSignatureAsync(lectureId, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("add-video/{lectureId}")]
        public async Task<ActionResult<ApiResponse>> AddVideo([FromRoute] string lectureId, [FromBody] AddMediaDTO addVideoDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.AddVideoToLectureAsync(lectureId, addVideoDTO.Name, addVideoDTO.Url, addVideoDTO.PublicId, addVideoDTO.Duration, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPut("update-video/{videoId}")]
        public async Task<ActionResult<ApiResponse>> UpdateVideo([FromRoute] string videoId, [FromBody] UpdateMediaDTO updateVideoDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.UpdateVideoAsync(videoId, updateVideoDTO.Name, updateVideoDTO.Url, updateVideoDTO.PublicId, updateVideoDTO.Duration, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPatch("update-video-orders")]
        public async Task<ActionResult<ApiResponse>> UpdateVideoOrders([FromBody] List<UpdateOrderDTO> videoOrders)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.UpdateVideoOrdersAsync(videoOrders, instructorId);
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
        [HttpGet("get-document/{documentId}")]
        public async Task<ActionResult<ApiResponse>> GetDocumentById([FromRoute] string documentId)
        {
            var response = await _lectureRepository.GetDocumentByIdAsync(documentId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("add-document/{lectureId}")]
        public async Task<ActionResult<ApiResponse>> AddDocument([FromRoute] string lectureId, [FromBody] AddMediaDTO addDocumentDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.AddDocumentToLectureAsync(lectureId, addDocumentDTO.Name, addDocumentDTO.Url, addDocumentDTO.PublicId, addDocumentDTO.Type ?? "", instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPut("update-document/{documentId}")]
        public async Task<ActionResult<ApiResponse>> UpdateDocument([FromRoute] string documentId, [FromBody] UpdateMediaDTO updateDocumentDTO)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.UpdateDocumentAsync(documentId, updateDocumentDTO.Name, updateDocumentDTO.Url, updateDocumentDTO.PublicId, updateDocumentDTO.Type, instructorId);
            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpDelete("delete-document/{documentId}")]
        public async Task<ActionResult<ApiResponse>> DeleteDocument([FromRoute] string documentId)
        {
            var instructorId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(instructorId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _lectureRepository.DeleteDocumentAsync(documentId, instructorId);
            return response.ToActionResult();
        }

    }
}