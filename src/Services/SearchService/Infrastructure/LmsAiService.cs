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
using Google.GenAI;
using Newtonsoft.Json;
using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using System.Net.Http;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System;
using System.Threading.Tasks;

namespace SearchService.Infrastructure
{
    public class LmsAiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _geminiApiKey;
        private readonly string _aiServerUrl;

        public LmsAiService(HttpClient httpClient)
        {
            _geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
            _aiServerUrl = Environment.GetEnvironmentVariable("AI_SERVER_URL") ?? "";
            _httpClient = httpClient;
        }

        public async Task<LmsAnalysisResponse> ProcessVideo(string cloudinaryUrl)
        {
            var requestData = new { url = cloudinaryUrl };
            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var aiResponse = await _httpClient.PostAsync(_aiServerUrl, content);
                
                if (!aiResponse.IsSuccessStatusCode)
                {
                    return new LmsAnalysisResponse { Summary = $"Transcription failed: {aiResponse.StatusCode}" };
                }

                var responseBody = await aiResponse.Content.ReadAsStringAsync();
                var transcribeResult = JsonConvert.DeserializeObject<TranscribeResponse>(responseBody);

                if (transcribeResult == null || transcribeResult.status != "success")
                {
                    return new LmsAnalysisResponse { Summary = "AI Server transcription returned error status." };
                }

                StringBuilder transcriptWithTimestamps = new StringBuilder();
                foreach (var segment in transcribeResult.segments)
                {
                    string timestamp = $"[{TimeSpan.FromSeconds(segment.start).ToString(@"mm\:ss")}]";
                    transcriptWithTimestamps.AppendLine($"{timestamp} {segment.text}");
                }

                var subtitles = transcribeResult.segments.Select(s => new SubtitleSegment
                {
                    StartTime = s.start,
                    EndTime = s.end,
                    Text = s.text
                }).ToList();

                // Gemini Analysis
                var analysis = await GetGeminiAnalysis(transcriptWithTimestamps.ToString());
                analysis.Subtitles = subtitles;

                return analysis;
            }
            catch (Exception ex)
            {
                return new LmsAnalysisResponse { Summary = $"Error in ProcessVideo: {ex.Message}" };
            }
        }

        private async Task<LmsAnalysisResponse> GetGeminiAnalysis(string timestampedTranscript)
        {
            if (string.IsNullOrWhiteSpace(timestampedTranscript)) return new LmsAnalysisResponse { Summary = "Không có nội dung để phân tích." };
            if (string.IsNullOrEmpty(_geminiApiKey)) throw new Exception("GEMINI_API_KEY is missing.");

            var client = new Google.GenAI.Client(apiKey: _geminiApiKey);

            string promptTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "SearchService", "Infrastructure", "Prompts", "LmsAnalysisPrompt.txt");
            if (!File.Exists(promptTemplatePath))
            {
                promptTemplatePath = Path.Combine("Services", "SearchService", "Infrastructure", "Prompts", "LmsAnalysisPrompt.txt");
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
    }
}



