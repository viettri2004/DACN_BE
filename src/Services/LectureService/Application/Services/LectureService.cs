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
using CourseService.Application.Interfaces;
using CourseService.Application.DTOs;
using Newtonsoft.Json;

using Hangfire;

namespace LectureService.Application.Services
{
    public class LectureService : ILectureService
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryService _cloudinaryService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IAiService _aiService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public LectureService(AppDbContext context, CloudinaryService cloudinaryService, IStringLocalizer<SharedResources> localizer, IAiService aiService, IBackgroundJobClient backgroundJobClient)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _localizer = localizer;
            _aiService = aiService;
            _backgroundJobClient = backgroundJobClient;
        }

        private async Task UpdateCourseTimestampAsync(string courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course != null)
            {
                course.UpdatedAt = DateTime.UtcNow;
                _context.Courses.Update(course);
            }
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
                await UpdateCourseTimestampAsync(createLectureDTO.CourseId);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["LectureCreated"].Value, lecture.Id, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> GetVideoUploadSignatureAsync(string lectureId, string instructorId)
        {
            return await _cloudinaryService.GetVideoUploadSignatureAsync(lectureId, instructorId);
        }

        public async Task<ApiResponse> AddVideoToLectureAsync(string lectureId, string name, string videoUrl, string publicId, double duration, string instructorId)
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

                var maxDisplayOrder = await _context.LectureVideos
                    .Where(v => v.LectureId == lectureId)
                    .MaxAsync(v => (int?)v.DisplayOrder) ?? 0;

                var lectureVideo = new LectureVideo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    VideoUrl = videoUrl,
                    PublicId = publicId,
                    Duration = duration,
                    DisplayOrder = maxDisplayOrder + 1,
                    LectureId = lectureId
                };

                _context.LectureVideos.Add(lectureVideo);
                await UpdateCourseTimestampAsync(lecture.CourseId);
                await _context.SaveChangesAsync();

                // Enqueue AI processing job immediately
                _backgroundJobClient.Enqueue<IVideoProcessingService>(x => x.ProcessVideoAsync(lectureVideo.Id));

                return new ApiResponse("Success", _localizer["VideoAdded"].Value, new { VideoId = lectureVideo.Id, Duration = duration }, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> UpdateLectureOrdersAsync(List<UpdateOrderDTO> lectureOrders, string instructorId)
        {
            try
            {
                var lectureIds = lectureOrders.Select(x => x.Id).ToList();
                
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

                    var newOrder = lectureOrders.First(x => x.Id == lecture.Id).DisplayOrder;
                    lecture.DisplayOrder = newOrder;
                }

                _context.Lectures.UpdateRange(lectures);
                await UpdateCourseTimestampAsync(lectures.First().CourseId);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["LectureUpdated"].Value, null, true);
            }
            catch (Exception ex)
            {
                 return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> UpdateVideoOrdersAsync(List<UpdateOrderDTO> videoOrders, string instructorId)
        {
            try
            {
                var videoIds = videoOrders.Select(x => x.Id).ToList();

                var videos = await _context.LectureVideos
                    .Include(v => v.Lecture)
                    .ThenInclude(l => l.Course)
                    .Where(v => videoIds.Contains(v.Id))
                    .ToListAsync();

                if (videos.Count != videoIds.Count)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                foreach (var video in videos)
                {
                    if (video.Lecture.Course.InstructorId != instructorId)
                    {
                        return new ApiResponse("Forbidden", _localizer["ForbiddenUpdateVideo"].Value, null, false);
                    }

                    var newOrder = videoOrders.First(x => x.Id == video.Id).DisplayOrder;
                    video.DisplayOrder = newOrder;
                }

                _context.LectureVideos.UpdateRange(videos);
                await UpdateCourseTimestampAsync(videos.First().Lecture.CourseId);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["VideoUpdated"].Value, null, true);
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
                var v = await _context.LectureVideos.AsNoTracking()
                    .Where(v => v.Id == videoId)
                    .FirstOrDefaultAsync();
                if (v == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                var lectureVideoDto = new LectureVideoDTO
                {
                    Name = v.Name,
                    VideoUrl = v.VideoUrl,
                    Duration = v.Duration,
                    SubtitleUrl = v.SubtitleUrl,
                    AnalysisResult = !string.IsNullOrEmpty(v.AnalysisResult) 
                       ? JsonConvert.DeserializeObject<LmsAnalysisResponse>(v.AnalysisResult) 
                       : null
                };

                return new ApiResponse("Success", _localizer["Success"].Value, lectureVideoDto, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> AddDocumentToLectureAsync(string lectureId, string name, string docUrl, string publicId, string type, string instructorId)
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

                var document = new Document
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Url = docUrl,
                    PublicId = publicId,
                    Type = type,
                    LectureId = lectureId
                };

                _context.Documents.Add(document);
                await UpdateCourseTimestampAsync(lecture.CourseId);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["DocumentAdded"].Value, document.Id, true);
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
                await UpdateCourseTimestampAsync(lecture.CourseId);
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
                    .Include(l => l.Documents)
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
                foreach (var document in lecture.Documents)
                {
                    if (!string.IsNullOrEmpty(document.PublicId))
                    {
                        try
                        {
                            await _cloudinaryService.DeleteDocumentAsync(document.PublicId);
                        }
                        catch
                        {

                        }
                    }
                }

                var lecturesToShift = await _context.Lectures
                    .Where(l => l.CourseId == lecture.CourseId && l.DisplayOrder > lecture.DisplayOrder)
                    .ToListAsync();

                foreach (var l in lecturesToShift)
                {
                    l.DisplayOrder--;
                }

                _context.Lectures.Remove(lecture);
                await UpdateCourseTimestampAsync(lecture.CourseId);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["LectureDeleted"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> DeleteDocumentAsync(string documentId, string instructorId)
        {
            try
            {
                var document = await _context.Documents
                    .Include(d => d.Lecture)
                    .ThenInclude(l => l.Course)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                if (document.Lecture.Course.InstructorId != instructorId)
                {
                    return new ApiResponse("Forbidden", _localizer["ForbiddenUpdateLecture"].Value, null, false);
                }

                if (!string.IsNullOrEmpty(document.PublicId))
                {
                    try
                    {
                        await _cloudinaryService.DeleteDocumentAsync(document.PublicId);
                    }
                    catch
                    {
                        
                    }
                }

                _context.Documents.Remove(document);
                await UpdateCourseTimestampAsync(document.Lecture.CourseId);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["DocumentDeleted"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> UpdateDocumentAsync(string documentId, string name, string? docUrl, string? publicId, string? type, string instructorId)
        {
            try
            {
                var document = await _context.Documents
                    .Include(d => d.Lecture)
                    .ThenInclude(l => l.Course)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                if (document.Lecture.Course.InstructorId != instructorId)
                {
                    return new ApiResponse("Forbidden", _localizer["ForbiddenUpdateLecture"].Value, null, false);
                }

                document.Name = name;

                if (!string.IsNullOrEmpty(docUrl) && !string.IsNullOrEmpty(publicId))
                {
                    if (!string.IsNullOrEmpty(document.PublicId))
                    {
                        try
                        {
                            await _cloudinaryService.DeleteDocumentAsync(document.PublicId);
                        }
                        catch
                        {
                            
                        }
                    }

                    document.Url = docUrl;
                    document.PublicId = publicId;
                    document.Type = type ?? string.Empty;
                }

                _context.Documents.Update(document);
                await UpdateCourseTimestampAsync(document.Lecture.CourseId);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["DocumentUpdated"].Value, document.Id, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> UpdateVideoAsync(string videoId, string name, string? videoUrl, string? publicId, double? duration, string instructorId)
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
                bool isNewVideo = false;

                if (!string.IsNullOrEmpty(videoUrl) && !string.IsNullOrEmpty(publicId))
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

                    video.VideoUrl = videoUrl;
                    video.PublicId = publicId;
                    video.Duration = duration ?? 0;
                    isNewVideo = true;
                }

                _context.LectureVideos.Update(video);
                await UpdateCourseTimestampAsync(video.Lecture.CourseId);
                await _context.SaveChangesAsync();

                if (isNewVideo)
                {
                    _backgroundJobClient.Enqueue<IVideoProcessingService>(x => x.ProcessVideoAsync(video.Id));
                }

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

                var videosToShift = await _context.LectureVideos
                    .Where(v => v.LectureId == video.LectureId && v.DisplayOrder > video.DisplayOrder)
                    .ToListAsync();

                foreach (var v in videosToShift)
                {
                    v.DisplayOrder--;
                }

                _context.LectureVideos.Remove(video);
                await UpdateCourseTimestampAsync(video.Lecture.CourseId);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["VideoDeleted"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> GetDocumentByIdAsync(string documentId)
        {
            try
            {
                var document = await _context.Documents.AsNoTracking()
                    .Where(d => d.Id == documentId)
                    .FirstOrDefaultAsync();
                if (document == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
                }

                if (!document.Url.Contains("/fl_attachment/"))
                {
                    document.Url = document.Url.Replace("/upload/", "/upload/fl_attachment/");
                }

                return new ApiResponse("Success", _localizer["Success"].Value, document.Url, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
    }
}