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
using System.Security.Cryptography;
using System.Text;
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
                string folder = "videos";
                string contextStr = $"lecture_id={lectureId}";
                string targetPreset = "lecture_video_preset";
                string stringToSign = $"context={contextStr}&folder={folder}&timestamp={timestamp}&upload_preset={targetPreset}";

                string apiSecret = _cloudinary.Api.Account.ApiSecret;

                using var sha1 = SHA1.Create();
                var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(stringToSign + apiSecret));
                string signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                var uploadCredentials = new
                {
                    Signature = signature,
                    Timestamp = timestamp,
                    Folder = folder,
                    Context = contextStr,
                    TargetPreset = targetPreset,
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
                if (payload.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogWarning("Webhook bị từ chối: Payload không phải là JSON Object. ValueKind hiện tại: {ValueKind}. Nội dung: {Content}", payload.ValueKind, payload.GetRawText());
                    return false;
                }
                if (!payload.TryGetProperty("notification_type", out var notifProp) ||
                    notifProp.ValueKind != JsonValueKind.String ||
                    notifProp.GetString() != "upload")
                {
                    return true;
                }

                if (!payload.TryGetProperty("resource_type", out var resTypeProp) ||
                    resTypeProp.ValueKind != JsonValueKind.String ||
                    resTypeProp.GetString() != "video")
                {
                    _logger.LogInformation("Webhook bị từ chối: File tải lên không phải là Video.");
                    return true;
                }

                string publicId = payload.TryGetProperty("public_id", out var pId) && pId.ValueKind == JsonValueKind.String ? pId.GetString()! : "";
                string secureUrl = payload.TryGetProperty("secure_url", out var sUrl) && sUrl.ValueKind == JsonValueKind.String ? sUrl.GetString()! : "";
                string originalFilename = payload.TryGetProperty("original_filename", out var fileProp) && fileProp.ValueKind == JsonValueKind.String ? fileProp.GetString()! : "Untitled Video";

                double duration = 0;
                if (payload.TryGetProperty("duration", out var durProp) && durProp.ValueKind == JsonValueKind.Number)
                {
                    duration = durProp.GetDouble();
                }

                if (string.IsNullOrEmpty(publicId) || string.IsNullOrEmpty(secureUrl))
                {
                    _logger.LogWarning("Webhook thiếu public_id hoặc secure_url.");
                    return false;
                }

                string? lectureId = null;

                if (payload.TryGetProperty("context", out var contextObj) && contextObj.ValueKind == JsonValueKind.Object)
                {
                    if (contextObj.TryGetProperty("custom", out var customObj) && customObj.ValueKind == JsonValueKind.Object)
                    {
                        if (customObj.TryGetProperty("lecture_id", out var lecIdProp) && lecIdProp.ValueKind == JsonValueKind.String)
                        {
                            lectureId = lecIdProp.GetString();
                        }
                    }
                    else if (contextObj.TryGetProperty("lecture_id", out var lecIdPropDirect) && lecIdPropDirect.ValueKind == JsonValueKind.String)
                    {
                        lectureId = lecIdPropDirect.GetString();
                    }
                }

                if (string.IsNullOrEmpty(lectureId))
                {
                    _logger.LogWarning("Không tìm thấy lecture_id trong context. Toàn bộ Payload từ Cloudinary: {Payload}", payload.GetRawText());
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
