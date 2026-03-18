using Google.Cloud.Speech.V2;
using Google.Cloud.Storage.V1;
using Google.GenAI;
using Newtonsoft.Json;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using System.Net.Http;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf;

namespace CourseService.Infrastructure
{
    public class LmsAiService : IAiService
    {
        private readonly string _projectId;
        private readonly string _location;
        private readonly string _recognizerId;
        private readonly string _bucketName;
        private readonly HttpClient _httpClient;
        private readonly string _geminiApiKey;

        public LmsAiService()
        {
            _projectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID") ?? "";
            _location = "us"; 
            _recognizerId = Environment.GetEnvironmentVariable("GOOGLE_RECOGNIZER_ID") ?? "lms-recognizer";
            _bucketName = Environment.GetEnvironmentVariable("GOOGLE_BUCKET_NAME") ?? "";
            _geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
            _httpClient = new HttpClient();
        }

        public async Task<LmsAnalysisResponse> ProcessVideo(string cloudinaryUrl)
        {
            if (string.IsNullOrEmpty(_projectId) || string.IsNullOrEmpty(_bucketName))
            {
                throw new Exception("Google Cloud configuration (ProjectId/BucketName) is missing in .env.");
            }

            string audioUrl = cloudinaryUrl.Replace(".mp4", ".mp3").Replace(".mov", ".mp3").Replace(".avi", ".mp3");
            string gcsUri = await UploadToGcs(audioUrl);
            string outputUri = $"gs://{_bucketName}/transcripts/results_{Guid.NewGuid()}/";

            try {
                var speechClient = await new SpeechClientBuilder
                {
                    Endpoint = "us-speech.googleapis.com"
                }.BuildAsync();

                string recognizerName = RecognizerName.Format(_projectId, _location, _recognizerId);

                var sttRequest = new BatchRecognizeRequest
                {
                    Recognizer = recognizerName,
                    Config = new RecognitionConfig
                    {
                        Features = new RecognitionFeatures 
                        { 
                            EnableAutomaticPunctuation = true,
                            EnableWordTimeOffsets = true 
                        },
                        AutoDecodingConfig = new AutoDetectDecodingConfig(),
                        LanguageCodes = { "vi-VN" },
                        Model = "chirp_3" 
                    },
                    Files = { new BatchRecognizeFileMetadata { Uri = gcsUri } },
                    RecognitionOutputConfig = new RecognitionOutputConfig
                    {
                        GcsOutputConfig = new GcsOutputConfig { Uri = outputUri }
                    }
                };

                var op = await speechClient.BatchRecognizeAsync(sttRequest);
                await op.PollUntilCompletedAsync();
                
                var results = await GetResultsFromGcs(outputUri);
                
                if (results == null || !results.Any()) {
                    return new LmsAnalysisResponse { Summary = "Không tìm thấy nội dung âm thanh hoặc lỗi nhận diện." };
                }

                StringBuilder transcriptWithTimestamps = new StringBuilder();
                var validResults = results.Where(r => r?.Alternatives != null && r.Alternatives.Any()).ToList();

                foreach (var result in validResults)
                {
                    var alternative = result.Alternatives[0];
                    if (alternative != null && !string.IsNullOrEmpty(alternative.Transcript))
                    {
                        string timestamp = "[00:00]";
                        if (alternative.Words != null && alternative.Words.Any() && alternative.Words[0].StartOffset != null)
                        {
                            timestamp = $"[{TimeSpan.FromSeconds(alternative.Words[0].StartOffset.Seconds).ToString(@"mm\:ss")}]";
                        }
                        transcriptWithTimestamps.AppendLine($"{timestamp} {alternative.Transcript}");
                    }
                }

                var allWords = validResults.SelectMany(r => r.Alternatives[0].Words)
                                           .Where(w => w != null && w.StartOffset != null && w.EndOffset != null)
                                           .ToList();

                var subtitles = allWords
                    .GroupBy(w => (int)(w.StartOffset.Seconds / 5)) 
                    .Select(g => new SubtitleSegment {
                        StartTime = g.First().StartOffset.Seconds + g.First().StartOffset.Nanos / 1e9,
                        EndTime = g.Last().EndOffset.Seconds + g.Last().EndOffset.Nanos / 1e9,
                        Text = string.Join(" ", g.Select(w => w.Word))
                    }).ToList();

                var analysis = await GetGeminiAnalysis(transcriptWithTimestamps.ToString());
                analysis.Subtitles = subtitles;

                return analysis;
            }
            catch (Exception ex) {
                throw new Exception($"Error in ProcessVideo: {ex.Message}", ex);
            }
        }

        private async Task<List<SpeechRecognitionResult>> GetResultsFromGcs(string outputUri)
        {
            var storage = await StorageClient.CreateAsync();
            var uri = new Uri(outputUri);
            string bucketName = uri.Host;
            string prefix = uri.AbsolutePath.TrimStart('/');

            var objects = storage.ListObjects(bucketName, prefix);
            var results = new List<SpeechRecognitionResult>();
            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

            foreach (var obj in objects)
            {
                if (obj.Name.EndsWith(".json"))
                {
                    using (var stream = new MemoryStream())
                    {
                        await storage.DownloadObjectAsync(bucketName, obj.Name, stream);
                        string jsonContent = Encoding.UTF8.GetString(stream.ToArray());
                        
                        var batchResults = parser.Parse<BatchRecognizeResults>(jsonContent);
                        if (batchResults?.Results != null)
                        {
                            results.AddRange(batchResults.Results);
                        }
                    }
                }
            }
            return results;
        }

        private async Task<LmsAnalysisResponse> GetGeminiAnalysis(string timestampedTranscript)
        {
            if (string.IsNullOrWhiteSpace(timestampedTranscript)) return new LmsAnalysisResponse { Summary = "Không có nội dung để phân tích." };
            if (string.IsNullOrEmpty(_geminiApiKey)) throw new Exception("GEMINI_API_KEY is missing.");

            var client = new Google.GenAI.Client(apiKey: _geminiApiKey);

            string promptTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "CourseService", "Infrastructure", "Prompts", "LmsAnalysisPrompt.txt");
            if (!File.Exists(promptTemplatePath))
            {
                promptTemplatePath = Path.Combine("Services", "CourseService", "Infrastructure", "Prompts", "LmsAnalysisPrompt.txt");
            }

            string promptTemplate = await File.ReadAllTextAsync(promptTemplatePath);
            string prompt = promptTemplate.Replace("{{transcript}}", timestampedTranscript);

            var response = await client.Models.GenerateContentAsync(
                model: "gemini-3-flash-preview",
                contents: prompt
            );

            string? jsonResponse = response.Candidates?[0]?.Content?.Parts?[0]?.Text;
            
            if (string.IsNullOrEmpty(jsonResponse))
            {
                return new LmsAnalysisResponse { Summary = "AI không trả về kết quả." };
            }

            jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();

            try {
                return JsonConvert.DeserializeObject<LmsAnalysisResponse>(jsonResponse) ?? new LmsAnalysisResponse();
            } catch {
                return new LmsAnalysisResponse { Summary = jsonResponse }; 
            }
        }

        private async Task<string> UploadToGcs(string url) 
        {
            var storage = await StorageClient.CreateAsync();
            string fileName = $"transcripts/audio_{Guid.NewGuid()}.mp3";

            using (var response = await _httpClient.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    await storage.UploadObjectAsync(_bucketName, fileName, "audio/mpeg", stream);
                }
            }

            return $"gs://{_bucketName}/{fileName}";
        }
    }
}
