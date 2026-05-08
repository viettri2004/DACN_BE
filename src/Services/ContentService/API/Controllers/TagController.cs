using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using OrderingService.Domain.Entities;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using LearningService.Application.Services;
using LearningService.Application.Interfaces;
using LearningService.Domain.Entities;
using InteractionService.Application.DTOs;
using InteractionService.Application.Interfaces;
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;

namespace ContentService.API.Controllers
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


