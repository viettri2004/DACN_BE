using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Shared.Infrastructure.cloudinaryService;

namespace CourseService.Infrastructure
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
