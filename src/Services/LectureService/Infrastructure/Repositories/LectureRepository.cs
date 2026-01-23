using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using Entities;
using LectureService.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using Shared.Infrastructure.cloudinaryService;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace LectureService.Infrastructure.Repositories
{
    public class LectureRepository : ILectureRepository
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryService _cloudinaryService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public LectureRepository(AppDbContext context, CloudinaryService cloudinaryService, IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _localizer = localizer;
        }

        public async Task<ApiResponse> AddVideoToLectureAsync(string lectureId, IFormFile videoFile)
        {
            try
            {
                var lecture = await _context.Lectures.FindAsync(lectureId);
                if (lecture == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                if (videoFile == null || videoFile.Length == 0)
                {
                    return new ApiResponse("BadRequest", "Video file is empty", null, false);
                }

                var (videoUrl, publicId) = await _cloudinaryService.UploadVideoAsync(videoFile);

                var lectureVideo = new LectureVideo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = videoFile.FileName,
                    VideoUrl = videoUrl,
                    LectureId = lectureId
                };

                _context.LectureVideos.Add(lectureVideo);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["Success"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
    }
}