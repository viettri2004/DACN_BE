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
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using System;
using System.Collections.Generic;
using InteractionService.Domain.Enums;

namespace InteractionService.Application.DTOs
{
    public class CommentDTO
    {
        public string CommentId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public int Rate { get; set; }
        public string Content { get; set; } = null!;
        public CommentType Type { get; set; }
        public bool IsMyComment { get; set; }
        public bool CanDelete { get; set; }
        public DateTime Timestamp { get; set; }
        public List<ReplyDTO> Replies { get; set; } = new List<ReplyDTO>();
    }
    public class ReplyDTO
    {
        public string CommentId { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public bool IsMyComment { get; set; }
        public bool CanDelete { get; set; }
        public CommentType Type { get; set; }
    }

    public class CourseCommentsResponseDTO
    {
        public bool IsInstructor { get; set; }
        public CommentDTO? MyComment { get; set; }
        public List<CommentDTO> AllComments { get; set; } = new List<CommentDTO>();
    }

    public class PagedCommentResultDTO : Shared.Domain.Entities.PagedResult<CommentDTO>
    {
        public double AverageRating { get; set; }
        public int TotalRatingCount { get; set; }
        public int Star5Count { get; set; }
        public int Star4Count { get; set; }
        public int Star3Count { get; set; }
        public int Star2Count { get; set; }
        public int Star1Count { get; set; }
    }

    public class AddCommentDTO
    {
        public string CourseId { get; set; } = null!;
        public int Rate { get; set; }
        public string Content { get; set; } = null!;
        public CommentType Type { get; set; } = CommentType.Review;
    }

    public class UpdateCommentDTO
    {
        public int Rate { get; set; }
        public string Content { get; set; } = null!;
    }

    public class AddReplyCommentDTO
    {
        public string ParentCommentId { get; set; } = null!;
        public string Content { get; set; } = null!;
        public CommentType Type { get; set; } = CommentType.Reply;
    }
}



