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
using System.Linq;
using System.Threading.Tasks;

namespace ContentService.Domain.Entities
{
    public class VideoSubtitle
    {
        public string Id { get; set; } = null!;
        public double StartTime { get; set; }     // Giây (vd: 12.5)
        public double EndTime { get; set; }       // Giây (vd: 15.8)
        public string Text { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public string LectureVideoId { get; set; } = null!;
        public LectureVideo LectureVideo { get; set; } = null!;
    }
}
