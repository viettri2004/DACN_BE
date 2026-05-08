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

namespace InteractionService.Application.DTOs
{
    public class QAThreadDTO
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string CreatorName { get; set; } = null!;
        public string? CreatorAvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public bool IsMyThread { get; set; }
        public int TotalMessages { get; set; }
        public bool IsUnread { get; set; }
    }

    public class QAMessageDTO
    {
        public string Id { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsMyMessage { get; set; }
        public bool IsInstructor { get; set; }
    }

    public class QAThreadDetailDTO
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string CreatorName { get; set; } = null!;
        public string? CreatorAvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsMyThread { get; set; }
    }

    public class CreateThreadDTO
    {
        public string CourseId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!; // First message content
    }

    public class AddMessageDTO
    {
        public string ThreadId { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    public class UpdateThreadDTO
    {
        public string Title { get; set; } = null!;
    }

    public class UpdateMessageDTO
    {
        public string Content { get; set; } = null!;
    }
}


