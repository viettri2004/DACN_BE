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
        Task<ApiResponse> CreateLectureAsync(CreateLectureDTO createLectureDTO);
        Task<ApiResponse> UpdateLectureAsync(string lectureId, UpdateLectureDTO updateLectureDTO);
        Task<ApiResponse> DeleteLectureAsync(string lectureId);
        Task<ApiResponse> AddVideoToLectureAsync(string lectureId, IFormFile videoFile);
        Task<ApiResponse> UpdateVideoAsync(string videoId, string name, IFormFile? videoFile);
        Task<ApiResponse> DeleteVideoAsync(string videoId);
        Task<ApiResponse> GetVideoByIdAsync(string videoId);
    }
}