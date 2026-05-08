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
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using System.Collections.Generic;

namespace SearchService.Application.DTOs
{
    public class LmsAnalysisResponse
    {
        public string Summary { get; set; } = string.Empty;
        public List<VideoSegment> Segments { get; set; } = new();
        public List<SubtitleSegment> Subtitles { get; set; } = new();
    }

    public class VideoSegment
    {
        public string StartTime { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SubtitleSegment
    {
        public double StartTime { get; set; } 
        public double EndTime { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}


