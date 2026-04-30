using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LectureService.Application.DTOs;
using Microsoft.AspNetCore.Http;
using src.Shared.Domain.Entities;

namespace LectureService.Application.Interfaces
{
    public interface ILectureRepository
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
    }
}