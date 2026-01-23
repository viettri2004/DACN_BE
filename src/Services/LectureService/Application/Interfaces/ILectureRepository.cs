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
        Task<ApiResponse> AddVideoToLectureAsync(string lectureId, IFormFile videoFile);
    }
}