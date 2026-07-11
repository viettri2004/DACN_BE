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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ContentService.Application.DTOs
{
    public class SubtitleSegmentDTO
    {
        public string? Id { get; set; }           // null khi tạo mới
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        [Required]
        public string Text { get; set; } = null!;
    }

    public class SaveSubtitlesDTO
    {
        [Required]
        public List<SubtitleSegmentDTO> Subtitles { get; set; } = new();
    }

    public class VideoSegmentDTO
    {
        [Required]
        public string StartTime { get; set; } = null!;
        [Required]
        public string Title { get; set; } = null!;
        [Required]
        public string Description { get; set; } = null!;
    }

    public class SaveVideoAnalysisDTO
    {
        [Required]
        public string Summary { get; set; } = null!;
        [Required]
        public List<VideoSegmentDTO> Segments { get; set; } = new();
    }
}
