using SearchService.Application.DTOs;
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
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using SearchService.Application.Services;
using SearchService.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using Shared.Infrastructure.cloudinaryService;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using ContentService.Application.Interfaces;
using ContentService.Application.DTOs;
using Newtonsoft.Json;

using Hangfire;

namespace ContentService.Application.Services
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

        public async Task<ApiResponse> GetSubtitlesAsync(string videoId, string instructorId)
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

                var subtitles = await _context.VideoSubtitles
                    .Where(s => s.LectureVideoId == videoId)
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new SubtitleSegmentDTO
                    {
                        Id = s.Id,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        Text = s.Text
                    })
                    .ToListAsync();

                if (!subtitles.Any())
                {
                    // 1. Try to download and migrate VTT file from Cloudinary
                    if (!string.IsNullOrEmpty(video.SubtitleUrl))
                    {
                        try
                        {
                            using var httpClient = new System.Net.Http.HttpClient();
                            var vttString = await httpClient.GetStringAsync(video.SubtitleUrl);
                            var parsedSubtitles = ParseVtt(vttString);
                            if (parsedSubtitles.Any())
                            {
                                int order = 0;
                                var listToSave = parsedSubtitles.Select(s => new VideoSubtitle
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    StartTime = s.StartTime,
                                    EndTime = s.EndTime,
                                    Text = s.Text,
                                    DisplayOrder = order++,
                                    LectureVideoId = videoId
                                }).ToList();

                                await _context.VideoSubtitles.AddRangeAsync(listToSave);
                                await _context.SaveChangesAsync();

                                subtitles = listToSave.Select(s => new SubtitleSegmentDTO
                                {
                                    Id = s.Id,
                                    StartTime = s.StartTime,
                                    EndTime = s.EndTime,
                                    Text = s.Text
                                }).ToList();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error migrating VTT for video {videoId}: {ex.Message}");
                        }
                    }

                    // 2. Fallback to AnalysisResult JSON if DB and VTT are not available
                    if (!subtitles.Any() && !string.IsNullOrEmpty(video.AnalysisResult))
                    {
                        try
                        {
                            var analysis = JsonConvert.DeserializeObject<SearchService.Application.DTOs.LmsAnalysisResponse>(video.AnalysisResult);
                            if (analysis != null && analysis.Subtitles != null && analysis.Subtitles.Any())
                            {
                                subtitles = analysis.Subtitles.Select(s => new SubtitleSegmentDTO
                                {
                                    Id = null,
                                    StartTime = s.StartTime,
                                    EndTime = s.EndTime,
                                    Text = s.Text
                                }).ToList();
                            }
                        }
                        catch
                        {
                            // Ignore JSON deserialization errors
                        }
                    }
                }

                return new ApiResponse("Success", _localizer["Success"].Value, subtitles, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> SaveSubtitlesAsync(string videoId, SaveSubtitlesDTO dto, string instructorId)
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

                if (dto == null || dto.Subtitles == null)
                {
                    return new ApiResponse("BadRequest", _localizer["InvalidSubtitlesData"].Value, null, false);
                }

                // Delete existing subtitles in DB
                var existingSubs = await _context.VideoSubtitles
                    .Where(s => s.LectureVideoId == videoId)
                    .ToListAsync();
                _context.VideoSubtitles.RemoveRange(existingSubs);

                var listToSave = new List<VideoSubtitle>();
                var listForVtt = new List<SearchService.Application.DTOs.SubtitleSegment>();
                int order = 0;

                foreach (var sub in dto.Subtitles.OrderBy(s => s.StartTime))
                {
                    var subId = string.IsNullOrEmpty(sub.Id) ? Guid.NewGuid().ToString() : sub.Id;
                    listToSave.Add(new VideoSubtitle
                    {
                        Id = subId,
                        StartTime = sub.StartTime,
                        EndTime = sub.EndTime,
                        Text = sub.Text,
                        DisplayOrder = order++,
                        LectureVideoId = videoId
                    });

                    listForVtt.Add(new SearchService.Application.DTOs.SubtitleSegment
                    {
                        StartTime = sub.StartTime,
                        EndTime = sub.EndTime,
                        Text = sub.Text
                    });
                }

                await _context.VideoSubtitles.AddRangeAsync(listToSave);

                // Regenerate VTT and upload to Cloudinary
                var vttContent = GenerateVtt(listForVtt);
                using (var vttStream = new System.IO.MemoryStream())
                {
                    using (var writer = new System.IO.StreamWriter(vttStream, new System.Text.UTF8Encoding(true)))
                    {
                        writer.Write(vttContent);
                        writer.Flush();
                        vttStream.Position = 0;
                        var (vttUrl, _) = await _cloudinaryService.UploadRawAsync(vttStream, $"subtitle_{video.Id}.vtt", "subtitles");
                        video.SubtitleUrl = vttUrl;
                    }
                }

                _context.LectureVideos.Update(video);
                await UpdateCourseTimestampAsync(video.Lecture.CourseId);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["SubtitlesSaved"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> SaveVideoAnalysisAsync(string videoId, SaveVideoAnalysisDTO dto, string instructorId)
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

                if (dto == null || dto.Summary == null || dto.Segments == null)
                {
                    return new ApiResponse("BadRequest", _localizer["InvalidAnalysisData"].Value, null, false);
                }

                // Retrieve existing analysis result or initialize a new one
                var analysis = new LmsAnalysisResponse();
                if (!string.IsNullOrEmpty(video.AnalysisResult))
                {
                    try
                    {
                        var existing = JsonConvert.DeserializeObject<LmsAnalysisResponse>(video.AnalysisResult);
                        if (existing != null)
                        {
                            analysis = existing;
                        }
                    }
                    catch
                    {
                        // Ignore deserialization error of invalid JSON
                    }
                }

                analysis.Summary = dto.Summary;
                analysis.Segments = dto.Segments.Select(s => new VideoSegment
                {
                    StartTime = s.StartTime,
                    Title = s.Title,
                    Description = s.Description
                }).ToList();

                video.AnalysisResult = JsonConvert.SerializeObject(analysis);

                _context.LectureVideos.Update(video);
                await UpdateCourseTimestampAsync(video.Lecture.CourseId);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["VideoAnalysisSaved"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> RetriggerAiProcessingAsync(string videoId, string instructorId)
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

                // Delete existing subtitles in DB
                var existingSubs = await _context.VideoSubtitles
                    .Where(s => s.LectureVideoId == videoId)
                    .ToListAsync();
                _context.VideoSubtitles.RemoveRange(existingSubs);

                // Clear AnalysisResult and SubtitleUrl
                video.AnalysisResult = null;
                video.SubtitleUrl = null;

                _context.LectureVideos.Update(video);
                await UpdateCourseTimestampAsync(video.Lecture.CourseId);
                await _context.SaveChangesAsync();

                // Enqueue Hangfire job
                _backgroundJobClient.Enqueue<IVideoProcessingService>(x => x.ProcessVideoAsync(video.Id));

                return new ApiResponse("Success", _localizer["AiProcessingRetriggered"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        private string GenerateVtt(List<SearchService.Application.DTOs.SubtitleSegment> subtitles)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("WEBVTT");
            sb.AppendLine();

            foreach (var subtitle in subtitles)
            {
                sb.AppendLine($"{FormatVttTime(subtitle.StartTime)} --> {FormatVttTime(subtitle.EndTime)}");
                sb.AppendLine(subtitle.Text);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string FormatVttTime(double seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
        }

        private List<SubtitleSegmentDTO> ParseVtt(string vttContent)
        {
            var list = new List<SubtitleSegmentDTO>();
            var lines = vttContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            double? currentStart = null;
            double? currentEnd = null;
            var currentTextBuilder = new System.Text.StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Contains("-->"))
                {
                    if (currentStart.HasValue && currentEnd.HasValue)
                    {
                        list.Add(new SubtitleSegmentDTO
                        {
                            StartTime = currentStart.Value,
                            EndTime = currentEnd.Value,
                            Text = currentTextBuilder.ToString().Trim()
                        });
                        currentTextBuilder.Clear();
                    }

                    var parts = line.Split(new[] { "-->" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        currentStart = ParseVttTime(parts[0].Trim());
                        currentEnd = ParseVttTime(parts[1].Trim());
                    }
                }
                else if (currentStart.HasValue && currentEnd.HasValue && !string.IsNullOrEmpty(line))
                {
                    if (currentTextBuilder.Length > 0)
                        currentTextBuilder.Append(" ");
                    currentTextBuilder.Append(line);
                }
            }

            if (currentStart.HasValue && currentEnd.HasValue)
            {
                list.Add(new SubtitleSegmentDTO
                {
                    StartTime = currentStart.Value,
                    EndTime = currentEnd.Value,
                    Text = currentTextBuilder.ToString().Trim()
                });
            }

            return list;
        }

        private double ParseVttTime(string timeStr)
        {
            var parts = timeStr.Split('.');
            var mainTime = parts[0];
            double ms = 0;
            if (parts.Length == 2)
            {
                double.TryParse(parts[1], out ms);
                ms = ms / Math.Pow(10, parts[1].Length);
            }

            var timeParts = mainTime.Split(':');
            double seconds = 0;
            if (timeParts.Length == 2)
            {
                double.TryParse(timeParts[0], out double m);
                double.TryParse(timeParts[1], out double s);
                seconds = m * 60 + s;
            }
            else if (timeParts.Length == 3)
            {
                double.TryParse(timeParts[0], out double h);
                double.TryParse(timeParts[1], out double m);
                double.TryParse(timeParts[2], out double s);
                seconds = h * 3600 + m * 60 + s;
            }

            return seconds + ms;
        }
    }
}



