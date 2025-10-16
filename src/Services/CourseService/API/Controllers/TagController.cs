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
    public class TagController : ControllerBase
    {
        private readonly ITagRepository _tagRepository;
        public TagController(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }
        [HttpPost("create-tags")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<ApiResponse>> CreateTag(CreateTagDTO createTagDTO)
        {
            var response = await _tagRepository.CreateTagAsync(createTagDTO);

            return response.ToActionResult();
        }

        [Authorize(Policy = "Instructor")]
        [HttpPost("assign-tag")]
        public async Task<ActionResult<ApiResponse>> AssignTagToCourse([FromBody] AssignTagToCourseDTO assignTagToCourseDTO)
        {
            var response = await _tagRepository.AssignTagToCourseAsync(assignTagToCourseDTO);

            return response.ToActionResult();
        }
        [HttpGet("all-tags")]
        public async Task<ActionResult<ApiResponse>> GetAllTags()
        {
            var response = await _tagRepository.GetAllTagsAsync();

            return response.ToActionResult();
        }

    }
}