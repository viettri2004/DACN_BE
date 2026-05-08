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

namespace OrderingService.Application.DTOs
{
    public class GiftCodeRedeemDto
    {
        public string Code { get; set; } = null!;
        public string? CourseId { get; set; } 
    }

    public class CreateGiftCodeDto
    {
        public string Code { get; set; } = null!;
        public string? CourseId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? MaxUses { get; set; }
    }

    public class UpdateGiftCodeDto
    {
        public string? Code { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? MaxUses { get; set; }
        public bool? IsActive { get; set; }
    }

    public class GiftCodeViewDto
    {
        public string Id { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? CourseId { get; set; }
        public int? MaxUses { get; set; }
        public int UsageCount { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}


