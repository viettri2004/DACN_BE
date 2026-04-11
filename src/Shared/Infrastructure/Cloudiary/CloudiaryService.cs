using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Data.Context;
using Entities;
using Google.GenAI;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using src.Shared.Resources;
using System.Text.Json;
using ApiResponse = src.Shared.Domain.Entities.ApiResponse;

namespace Shared.Infrastructure.cloudinaryService
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly AppDbContext _context;
        private readonly ILogger<CloudinaryService> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public CloudinaryService(Cloudinary cloudinary, AppDbContext context, ILogger<CloudinaryService> logger, IStringLocalizer<SharedResources> localizer)
        {
            _cloudinary = cloudinary;
            _context = context;
            _logger = logger;
            _localizer = localizer;
        }

        public async Task<ApiResponse> GetVideoUploadSignatureAsync(string lectureId, string instructorId)
        {
            try
            {
                var lecture = await _context.Lectures.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == lectureId);
                if (lecture == null)
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

                if (lecture.Course.InstructorId != instructorId)
                    return new ApiResponse("Forbidden", _localizer["ForbiddenAddVideo"].Value, null, false);

                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string folder = "vietedu/lectures/videos";
                string contextStr = $"lecture_id={lectureId}";

                var parametersToSign = new Dictionary<string, object>
                {
                    { "timestamp", timestamp },
                    { "folder", folder },
                    { "context", contextStr }
                };

                string signature = _cloudinary.Api.SignParameters(parametersToSign);

                var uploadCredentials = new
                {
                    Signature = signature,
                    Timestamp = timestamp,
                    Folder = folder,
                    Context = contextStr,
                    ApiKey = _cloudinary.Api.Account.ApiKey,
                    CloudName = _cloudinary.Api.Account.Cloud
                };

                return new ApiResponse("Success", "Signature generated", uploadCredentials, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating upload signature");
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<bool> ProcessCloudinaryWebhookAsync(JsonElement payload)
        {
            try
            {
                if (!payload.TryGetProperty("notification_type", out var notifProp) || notifProp.GetString() != "upload")
                    return true;

                var publicId = payload.GetProperty("public_id").GetString();
                var secureUrl = payload.GetProperty("secure_url").GetString();
                var duration = payload.TryGetProperty("duration", out var durProp) ? durProp.GetDouble() : 0;
                var originalFilename = payload.TryGetProperty("original_filename", out var fileProp) ? fileProp.GetString() : "Untitled Video";

                string? lectureId = null;
                if (payload.TryGetProperty("context", out var contextObj) &&
                    contextObj.TryGetProperty("custom", out var customObj) &&
                    customObj.TryGetProperty("lecture_id", out var lectureIdProp))
                {
                    lectureId = lectureIdProp.GetString();
                }

                if (string.IsNullOrEmpty(lectureId))
                {
                    _logger.LogWarning("Cloudinary webhook received for public_id {PublicId} but no lecture_id in context", publicId);
                    return false;
                }

                var maxDisplayOrder = await _context.LectureVideos
                    .Where(v => v.LectureId == lectureId)
                    .MaxAsync(v => (int?)v.DisplayOrder) ?? 0;

                var lectureVideo = new LectureVideo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = originalFilename,
                    VideoUrl = secureUrl,
                    PublicId = publicId,
                    Duration = duration,
                    DisplayOrder = maxDisplayOrder + 1,
                    LectureId = lectureId
                };

                _context.LectureVideos.Add(lectureVideo);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully processed Cloudinary upload for lecture {LectureId}, Video: {PublicId}", lectureId, publicId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Cloudinary Webhook");
                return false;
            }
        }

        public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "uploads" 
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
                return (result.SecureUrl.ToString(), result.PublicId);

            throw new Exception($"{result.Error?.Message}");
        }

        public async Task DeleteImageAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"{result.Error?.Message}");
        }

        public async Task<(string Url, string PublicId, double Duration)> UploadVideoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            using var stream = file.OpenReadStream();

            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "videos"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
                return (result.SecureUrl.ToString(), result.PublicId, result.Duration);

            throw new Exception($"{result.Error?.Message}");
        }

        public async Task DeleteVideoAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Video
            };
            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Result != "ok")
                throw new Exception($"{result.Error?.Message}");
        }

        public async Task<(string Url, string PublicId)> UploadDocumentAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "documents"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
                return (result.SecureUrl.ToString(), result.PublicId);

            throw new Exception($"{result.Error?.Message}");
        }

        public async Task<(string Url, string PublicId)> UploadRawAsync(System.IO.Stream stream, string fileName, string folder)
        {
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folder
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
                return (result.SecureUrl.ToString(), result.PublicId);

            throw new Exception($"{result.Error?.Message}");
        }

        public async Task DeleteDocumentAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Raw
            };
            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Result != "ok")
                throw new Exception($"{result.Error?.Message}");
        }
    }
}
