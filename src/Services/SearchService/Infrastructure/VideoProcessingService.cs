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
using SearchService.Application.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Shared.Infrastructure.cloudinaryService;

namespace SearchService.Infrastructure
{
    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly AppDbContext _context;
        private readonly IAiService _aiService;
        private readonly CloudinaryService _cloudinaryService;

        public VideoProcessingService(AppDbContext context, IAiService aiService, CloudinaryService cloudinaryService)
        {
            _context = context;
            _aiService = aiService;
            _cloudinaryService = cloudinaryService;
        }

        public async Task ProcessVideoAsync(string videoId)
        {
            var video = await _context.LectureVideos.FindAsync(videoId);
            if (video == null) return;

            try
            {
                var aiResult = await _aiService.ProcessVideo(video.VideoUrl);

                if (aiResult.Subtitles != null && aiResult.Subtitles.Any())
                {
                    var vttContent = GenerateVtt(aiResult.Subtitles);

                    using var vttStream = new MemoryStream();
                    using (var writer = new StreamWriter(vttStream, new UTF8Encoding(true)))
                    {
                        writer.Write(vttContent);
                        writer.Flush();
                        vttStream.Position = 0;

                        var (vttUrl, _) = await _cloudinaryService.UploadRawAsync(vttStream, $"subtitle_{video.Id}.vtt", "subtitles");
                        video.SubtitleUrl = vttUrl;
                    }
                }

                var dbAnalysis = new
                {
                    Summary = aiResult.Summary,
                    Segments = aiResult.Segments
                };
                video.AnalysisResult = JsonConvert.SerializeObject(dbAnalysis);

                // Sync subtitles to DB
                if (aiResult.Subtitles != null)
                {
                    var existingSubs = await _context.VideoSubtitles
                        .Where(s => s.LectureVideoId == videoId).ToListAsync();
                    _context.VideoSubtitles.RemoveRange(existingSubs);

                    int order = 0;
                    foreach (var sub in aiResult.Subtitles)
                    {
                        _context.VideoSubtitles.Add(new VideoSubtitle
                        {
                            Id = Guid.NewGuid().ToString(),
                            StartTime = sub.StartTime,
                            EndTime = sub.EndTime,
                            Text = sub.Text,
                            DisplayOrder = order++,
                            LectureVideoId = videoId
                        });
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing video {videoId}: {ex.Message}");
                throw;
            }
        }

        private string GenerateVtt(List<SubtitleSegment> subtitles)
        {
            var sb = new StringBuilder();
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
    }
}



