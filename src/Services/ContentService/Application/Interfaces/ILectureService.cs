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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentService.Application.DTOs;
using Microsoft.AspNetCore.Http;
using src.Shared.Domain.Entities;

namespace ContentService.Application.Interfaces
{
    public interface ILectureService
    {
        Task<ApiResponse> CreateLectureAsync(CreateLectureDTO createLectureDTO, string instructorId);
        Task<ApiResponse> UpdateLectureAsync(string lectureId, UpdateLectureDTO updateLectureDTO, string instructorId);
        Task<ApiResponse> DeleteLectureAsync(string lectureId, string instructorId);
        Task<ApiResponse> GetVideoUploadSignatureAsync(string lectureId, string instructorId);
        Task<ApiResponse> AddVideoToLectureAsync(string lectureId, string name, string videoUrl, string publicId, double duration, string instructorId);
        Task<ApiResponse> UpdateVideoAsync(string videoId, string name, string? videoUrl, string? publicId, double? duration, string instructorId);
        Task<ApiResponse> DeleteVideoAsync(string videoId, string instructorId);
        Task<ApiResponse> UpdateLectureOrdersAsync(List<UpdateOrderDTO> lectureOrders, string instructorId);
        Task<ApiResponse> GetVideoByIdAsync(string videoId);
        Task<ApiResponse> AddDocumentToLectureAsync(string lectureId, string name, string docUrl, string publicId, string type, string instructorId);
        Task<ApiResponse> DeleteDocumentAsync(string documentId, string instructorId);
        Task<ApiResponse> UpdateDocumentAsync(string documentId, string name, string? docUrl, string? publicId, string? type, string instructorId);
        Task<ApiResponse> UpdateVideoOrdersAsync(List<UpdateOrderDTO> videoOrders, string instructorId);
        Task<ApiResponse> GetDocumentByIdAsync(string documentId);
        Task<ApiResponse> GetSubtitlesAsync(string videoId, string instructorId);
        Task<ApiResponse> SaveSubtitlesAsync(string videoId, SaveSubtitlesDTO dto, string instructorId);
        Task<ApiResponse> RetriggerAiProcessingAsync(string videoId, string instructorId);
        Task<ApiResponse> SaveVideoAnalysisAsync(string videoId, SaveVideoAnalysisDTO dto, string instructorId);
    }
}


