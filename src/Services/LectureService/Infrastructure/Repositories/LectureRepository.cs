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

        public async Task<ApiResponse> CreateLectureAsync(CreateLectureDTO createLectureDTO, string instructorId)
        {
            try
            {
                var course = await _context.Courses.FindAsync(createLectureDTO.CourseId);
                if (course == null)
                {
                    return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);
                }

                if (course.InstructorId != instructorId)
                {
                    return new ApiResponse("Forbidden", _localizer["ForbiddenAddLecture"].Value, null, false);
                }

                int newDisplayOrder;

                if (createLectureDTO.DisplayOrder.HasValue)
                {
                    newDisplayOrder = createLectureDTO.DisplayOrder.Value;
                    var lecturesToShift = await _context.Lectures
                        .Where(l => l.CourseId == createLectureDTO.CourseId && l.DisplayOrder >= newDisplayOrder)
                        .ToListAsync();
                    
                    foreach (var l in lecturesToShift)
                    {
                        l.DisplayOrder++;
                    }
                }
                else
                {
                    var maxDisplayOrder = await _context.Lectures
                        .Where(l => l.CourseId == createLectureDTO.CourseId)
                        .MaxAsync(l => (int?)l.DisplayOrder) ?? 0;
                    newDisplayOrder = maxDisplayOrder + 1;
                }

                var lecture = new Lecture
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = createLectureDTO.Name,
                    Description = createLectureDTO.Description,
                    CourseId = createLectureDTO.CourseId,
                    DisplayOrder = newDisplayOrder
                };
                
                _context.Lectures.Add(lecture);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["LectureCreated"].Value, lecture.Id, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> AddVideoToLectureAsync(string lectureId, IFormFile videoFile, string instructorId)
        {
            try
            {
                var lecture = await _context.Lectures.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == lectureId);
                if (lecture == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                if (lecture.Course.InstructorId != instructorId)
                {
                    return new ApiResponse("Forbidden", _localizer["ForbiddenAddVideo"].Value, null, false);
                }

                if (videoFile == null || videoFile.Length == 0)
                {
                    return new ApiResponse("BadRequest", _localizer["VideoFileEmpty"].Value, null, false);
                }

                var (videoUrl, publicId, duration) = await _cloudinaryService.UploadVideoAsync(videoFile);

                var lectureVideo = new LectureVideo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = videoFile.FileName,
                    VideoUrl = videoUrl,
                    PublicId = publicId,
                    Duration = duration,
                    LectureId = lectureId
                };

                _context.LectureVideos.Add(lectureVideo);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["VideoAdded"].Value, new { VideoId = lectureVideo.Id, Duration = duration }, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
        public async Task<ApiResponse> UpdateLectureOrdersAsync(List<LectureOrderDTO> lectureOrders, string instructorId)
        {
            try
            {
                var lectureIds = lectureOrders.Select(x => x.LectureId).ToList();
                
                var lectures = await _context.Lectures
                    .Include(l => l.Course)
                    .Where(l => lectureIds.Contains(l.Id))
                    .ToListAsync();

                if (lectures.Count != lectureIds.Count)
                {
                     return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                foreach (var lecture in lectures)
                {
                    if (lecture.Course.InstructorId != instructorId)
                    {
                         return new ApiResponse("Forbidden", _localizer["ForbiddenUpdateLecture"].Value, null, false);
                    }

                    var newOrder = lectureOrders.First(x => x.LectureId == lecture.Id).DisplayOrder;
                    lecture.DisplayOrder = newOrder;
                }

                _context.Lectures.UpdateRange(lectures);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["LectureUpdated"].Value, null, true);
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
                        VideoUrl = v.VideoUrl,
                        Duration = v.Duration
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

        public async Task<ApiResponse> UpdateLectureAsync(string lectureId, UpdateLectureDTO updateLectureDTO, string instructorId)
        {
            try
            {
                var lecture = await _context.Lectures.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == lectureId);
                if (lecture == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                if (lecture.Course.InstructorId != instructorId)
                {
                    return new ApiResponse("Forbidden", _localizer["ForbiddenUpdateLecture"].Value, null, false);
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

        public async Task<ApiResponse> DeleteLectureAsync(string lectureId, string instructorId)
        {
            try
            {
                var lecture = await _context.Lectures
                    .Include(l => l.LectureVideos)
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.Id == lectureId);

                if (lecture == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                if (lecture.Course.InstructorId != instructorId)
                {
                    return new ApiResponse("Forbidden", _localizer["ForbiddenDeleteLecture"].Value, null, false);
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

        public async Task<ApiResponse> UpdateVideoAsync(string videoId, string name, IFormFile? videoFile, string instructorId)
        {
            try
            {
                var video = await _context.LectureVideos
                    .Include(v => v.Lecture)
                    .ThenInclude(l => l.Course)
                    .FirstOrDefaultAsync(v => v.Id == videoId);
                    
                if (video == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                if (video.Lecture.Course.InstructorId != instructorId)
                {
                    return new ApiResponse("Forbidden", _localizer["ForbiddenUpdateVideo"].Value, null, false);
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
                            
                        }
                    }

                    var (videoUrl, publicId, duration) = await _cloudinaryService.UploadVideoAsync(videoFile);
                    video.VideoUrl = videoUrl;
                    video.PublicId = publicId;
                    video.Duration = duration;
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

        public async Task<ApiResponse> DeleteVideoAsync(string videoId, string instructorId)
        {
            try
            {
                var video = await _context.LectureVideos
                    .Include(v => v.Lecture)
                    .ThenInclude(l => l.Course)
                    .FirstOrDefaultAsync(v => v.Id == videoId);
                
                if (video == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                if (video.Lecture.Course.InstructorId != instructorId)
                {
                    return new ApiResponse("Forbidden", _localizer["ForbiddenDeleteVideo"].Value, null, false);
                }

                if (!string.IsNullOrEmpty(video.PublicId))
                {
                     try
                     {
                         await _cloudinaryService.DeleteVideoAsync(video.PublicId);
                     }
                     catch
                     {
                         
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