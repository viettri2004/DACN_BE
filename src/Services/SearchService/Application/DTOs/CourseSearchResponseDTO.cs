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
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using ContentService.Application.DTOs;
using System.Collections.Generic;
using Shared.Domain.Entities;

namespace SearchService.Application.DTOs
{
    public class CourseSearchResponseDTO
    {
        public PagedResult<CourseCardDTO> Courses { get; set; } = default!;
        public List<TagFacetDTO> AvailableTags { get; set; } = new();
        public string? DidYouMean { get; set; }
    }
}



