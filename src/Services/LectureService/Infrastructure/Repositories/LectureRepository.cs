using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using Entities;
using LectureService.Application.DTOs;
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

        public async Task<ApiResponse> CreateLectureAsync(CreateLectureDTO createLectureDTO)
        {
            try
            {
                var course = await _context.Courses.FindAsync(createLectureDTO.CourseId);
                if (course == null)
                {
                    return new ApiResponse("NotFound", "Course not found", null, false);
                }

                var lecture = new Lecture
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = createLectureDTO.Name,
                    Description = createLectureDTO.Description,
                    CourseId = createLectureDTO.CourseId
                };

                return new ApiResponse("Success", _localizer["LectureCreated"].Value, lecture.Id, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
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
                    PublicId = publicId,
                    LectureId = lectureId
                };

                _context.LectureVideos.Add(lectureVideo);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["VideoAdded"].Value, lectureVideo.Id, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
        public async Task<ApiResponse> GetVideoByIdAsync(string videoId)
        {
            try
            {
                var lectureVideo = await _context.LectureVideos.AsNoTracking()
                    .Where(v => v.Id == videoId)
                    .Select(v => new LectureVideoDTO
                    {
                        Name = v.Name,
                        VideoUrl = v.VideoUrl
                    })
                    .FirstOrDefaultAsync();
                if (lectureVideo == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                return new ApiResponse("Success", _localizer["Success"].Value, lectureVideo, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> UpdateLectureAsync(string lectureId, UpdateLectureDTO updateLectureDTO)
        {
            try
            {
                var lecture = await _context.Lectures.FindAsync(lectureId);
                if (lecture == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                lecture.Name = updateLectureDTO.Name;
                lecture.Description = updateLectureDTO.Description;

                _context.Lectures.Update(lecture);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["LectureUpdated"].Value, lecture.Id, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> DeleteLectureAsync(string lectureId)
        {
            try
            {
                var lecture = await _context.Lectures
                    .Include(l => l.LectureVideos)
                    .FirstOrDefaultAsync(l => l.Id == lectureId);

                if (lecture == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                foreach (var video in lecture.LectureVideos)
                {
                    if (!string.IsNullOrEmpty(video.PublicId))
                    {
                        try
                        {
                            await _cloudinaryService.DeleteVideoAsync(video.PublicId);
                        }
                        catch
                        {
                            // Continue deleting other videos and the lecture even if one video deletion fails
                            // Ideally log this
                        }
                    }
                }

                _context.Lectures.Remove(lecture);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["LectureDeleted"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> UpdateVideoAsync(string videoId, string name, IFormFile? videoFile)
        {
            try
            {
                var video = await _context.LectureVideos.FindAsync(videoId);
                if (video == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                video.Name = name;

                if (videoFile != null && videoFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(video.PublicId))
                    {
                        try
                        {
                            await _cloudinaryService.DeleteVideoAsync(video.PublicId);
                        }
                        catch
                        {
                            // Log and continue
                        }
                    }

                    var (videoUrl, publicId) = await _cloudinaryService.UploadVideoAsync(videoFile);
                    video.VideoUrl = videoUrl;
                    video.PublicId = publicId;
                }

                _context.LectureVideos.Update(video);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["VideoUpdated"].Value, video.Id, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> DeleteVideoAsync(string videoId)
        {
            try
            {
                var video = await _context.LectureVideos.FindAsync(videoId);
                if (video == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                if (!string.IsNullOrEmpty(video.PublicId))
                {
                     try
                     {
                         await _cloudinaryService.DeleteVideoAsync(video.PublicId);
                     }
                     catch
                     {
                         // Log and continue
                     }
                }

                _context.LectureVideos.Remove(video);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["VideoDeleted"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
    }
}