using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using src.Shared.Infrastructure;

using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace ContentService.Application.Services
{
    public class QuizService : IQuizService
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IDistributedCache _cache;

        public QuizService(AppDbContext context, IStringLocalizer<SharedResources> localizer, IDistributedCache cache)
        {
            _context = context;
            _localizer = localizer;
            _cache = cache;
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

            public async Task<ApiResponse> CreateQuizAsync(CreateQuizDTO createQuizDTO, string instructorId)
        {
            try
            {
                var lecture = await _context.Lectures
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.Id == createQuizDTO.LectureId);

                if (lecture == null)
                    return new ApiResponse("NotFound", _localizer["LectureNotFound"].Value, null, false);

                if (lecture.Course.InstructorId != instructorId)
                    return new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false);

                var quiz = new Quiz
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = createQuizDTO.Name,
                    LectureId = createQuizDTO.LectureId,
                    TestTime = createQuizDTO.TestTime,
                    // AttemptCount = createQuizDTO.AttemptCount,
                    Questions = new List<Question>()
                };

                if (createQuizDTO.Questions != null && createQuizDTO.Questions.Any())
                {
                    foreach (var qDto in createQuizDTO.Questions)
                    {
                        var question = new Question
                        {
                            Id = Guid.NewGuid().ToString(),
                            QuizId = quiz.Id,
                            Content = qDto.Content,
                            DisplayOrder = qDto.DisplayOrder,
                            Explanation = qDto.Explanation,
                            QuestionOptions = new List<QuestionOption>()
                        };

                        question.ImageUrl = qDto.ImageUrl;
                        question.ImagePublicId = qDto.ImagePublicId;

                        if (qDto.Options != null)
                        {
                            foreach (var oDto in qDto.Options)
                            {
                                question.QuestionOptions.Add(new QuestionOption
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    QuestionId = question.Id,
                                    Content = oDto.Content,
                                    IsCorrect = oDto.IsCorrect,
                                    DisplayOrder = oDto.DisplayOrder
                                });
                            }
                        }
                        quiz.Questions.Add(question);
                    }
                }

                _context.Quizzes.Add(quiz);
                await UpdateCourseTimestampAsync(lecture.CourseId);
                await _context.SaveChangesAsync();

                return new ApiResponse("Created", _localizer["CreateQuizSuccess"].Value, quiz.Id, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating quiz: {ex.Message}");
                return new ApiResponse("Error", _localizer["CreateQuizFailed"].Value, null, false);
            }
        }

        public async Task<ApiResponse> UpdateQuizAsync(string quizId, UpdateQuizDTO updateQuizDTO, string instructorId)
        {
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Lecture)
                        .ThenInclude(l => l.Course)
                    .Include(q => q.Questions)
                        .ThenInclude(qs => qs.QuestionOptions)
                    .FirstOrDefaultAsync(q => q.Id == quizId);

                if (quiz == null)
                    return new ApiResponse("NotFound", _localizer["QuizNotFound"].Value, null, false);

                if (quiz.Lecture.Course.InstructorId != instructorId)
                    return new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false);

                if (!string.IsNullOrEmpty(updateQuizDTO.Name))
                    quiz.Name = updateQuizDTO.Name;
                
                if (updateQuizDTO.TestTime.HasValue)
                    quiz.TestTime = updateQuizDTO.TestTime.Value;

                // if (updateQuizDTO.AttemptCount.HasValue)
                //     quiz.AttemptCount = updateQuizDTO.AttemptCount.Value;

                if (updateQuizDTO.Questions != null)
                {
                    // DETACH old questions instead of removing them to maintain historical integrity for QuizAttemptAnswers
                    foreach (var oldQuestion in quiz.Questions.ToList())
                    {
                        oldQuestion.QuizId = null;
                    }
                    
                    foreach (var qDto in updateQuizDTO.Questions)
                    {
                        var question = new Question
                        {
                            Id = Guid.NewGuid().ToString(),
                            QuizId = quiz.Id,
                            Content = qDto.Content,
                            DisplayOrder = qDto.DisplayOrder,
                            Explanation = qDto.Explanation,
                            QuestionOptions = new List<QuestionOption>()
                        };

                        question.ImageUrl = qDto.ImageUrl;
                        question.ImagePublicId = qDto.ImagePublicId;

                        if (qDto.Options != null)
                        {
                            foreach (var oDto in qDto.Options)
                            {
                                question.QuestionOptions.Add(new QuestionOption
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    QuestionId = question.Id,
                                    Content = oDto.Content,
                                    IsCorrect = oDto.IsCorrect,
                                    DisplayOrder = oDto.DisplayOrder
                                });
                            }
                        }
                        _context.Questions.Add(question);
                    }
                }

                await UpdateCourseTimestampAsync(quiz.Lecture.CourseId);
                await _context.SaveChangesAsync();
                await RemoveQuizCache(quizId);

                return new ApiResponse("Success", _localizer["UpdateQuizSuccess"].Value, null, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating quiz: {ex.Message}");
                return new ApiResponse("Error", _localizer["UpdateQuizFailed"].Value, null, false);
            }
        }

        public async Task<ApiResponse> DeleteQuizAsync(string quizId, string instructorId)
        {
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Lecture)
                        .ThenInclude(l => l.Course)
                    .Include(q => q.Questions)
                    .Include(q => q.QuizAttempts)
                    .FirstOrDefaultAsync(q => q.Id == quizId);

                if (quiz == null)
                    return new ApiResponse("NotFound", _localizer["QuizNotFound"].Value, null, false);

                if (quiz.Lecture.Course.InstructorId != instructorId)
                    return new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false);

                foreach (var question in quiz.Questions)
                {
                    question.QuizId = null;
                }

                // Delete all attempts related to the quiz
                _context.QuizAttempts.RemoveRange(quiz.QuizAttempts);

                _context.Quizzes.Remove(quiz);
                await UpdateCourseTimestampAsync(quiz.Lecture.CourseId);
                await _context.SaveChangesAsync();
                await RemoveQuizCache(quizId);

                return new ApiResponse("Success", _localizer["DeleteQuizSuccess"].Value, null, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting quiz: {ex.Message}");
                return new ApiResponse("Error", _localizer["DeleteQuizFailed"].Value, null, false);
            }
        }

        public async Task<ApiResponse> GetQuizByIdAsync(string quizId)
        {
            string cacheKey = $"quiz:{quizId}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<ApiResponse>(cachedData, JsonSettings.CamelCase);
            }

            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Questions)
                        .ThenInclude(qs => qs.QuestionOptions)
                    .FirstOrDefaultAsync(q => q.Id == quizId);

                if (quiz == null)
                    return new ApiResponse("NotFound", _localizer["QuizNotFound"].Value, null, false);

                var quizDto = new QuizDTO
                {
                    Id = quiz.Id,
                    Name = quiz.Name,
                    LectureId = quiz.LectureId,
                    TestTime = quiz.TestTime,
                    // AttemptCount = quiz.AttemptCount,
                    Questions = quiz.Questions.Select(q => new QuestionDTO
                    {
                        Id = q.Id,
                        Content = q.Content,
                        DisplayOrder = q.DisplayOrder,
                        Explanation = q.Explanation,
                        ImageUrl = q.ImageUrl,
                        ImagePublicId = q.ImagePublicId,
                        Options = q.QuestionOptions.Select(o => new QuestionOptionDTO
                        {
                            Id = o.Id,
                            Content = o.Content,
                            IsCorrect = o.IsCorrect,
                            DisplayOrder = o.DisplayOrder
                        }).OrderBy(o => o.DisplayOrder).ToList()
                    }).OrderBy(q => q.DisplayOrder).ToList()
                };

                var response = new ApiResponse("Success", _localizer["Success"].Value, quizDto, true);

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                };
                await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response, JsonSettings.CamelCase), cacheOptions);

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting quiz: {ex.Message}");
                return new ApiResponse("Error", _localizer["Error"].Value, null, false);
            }
        }

        private async Task RemoveQuizCache(string quizId)
        {
            await _cache.RemoveAsync($"quiz:{quizId}");
        }

        public async Task<ApiResponse> StartQuizAttemptAsync(string quizId, string studentId)
        {
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Lecture)
                        .ThenInclude(l => l.Course)
                    .Include(q => q.Questions)
                        .ThenInclude(qs => qs.QuestionOptions)
                    .FirstOrDefaultAsync(q => q.Id == quizId);

                if (quiz == null)
                    return new ApiResponse("NotFound", _localizer["QuizNotFound"].Value, null, false);

                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == quiz.Lecture.CourseId);

                if (enrollment == null)
                    return new ApiResponse("Forbidden", _localizer["NotEnrolled"].Value, null, false);

                var attempt = new QuizAttempt
                {
                    Id = Guid.NewGuid().ToString(),
                    QuizId = quizId,
                    EnrollmentId = enrollment.Id,
                    AttemptedAt = DateTime.UtcNow,
                    Score = 0
                };

                _context.QuizAttempts.Add(attempt);
                await _context.SaveChangesAsync();

                var attemptDto = new QuizAttemptResponseDTO
                {
                    AttemptId = attempt.Id,
                    QuizId = quiz.Id,
                    QuizName = quiz.Name,
                    TestTime = quiz.TestTime,
                    Questions = quiz.Questions.Select(q => new QuestionDTO
                    {
                        Id = q.Id,
                        Content = q.Content,
                        DisplayOrder = q.DisplayOrder,
                        ImageUrl = q.ImageUrl,
                        ImagePublicId = q.ImagePublicId,
                        Options = q.QuestionOptions.Select(o => new QuestionOptionDTO
                        {
                            Id = o.Id,
                            Content = o.Content,
                            DisplayOrder = o.DisplayOrder
                        }).OrderBy(o => o.DisplayOrder).ToList()
                    }).OrderBy(q => q.DisplayOrder).ToList()
                };

                return new ApiResponse("Success", _localizer["Success"].Value, attemptDto, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting quiz attempt: {ex.Message}");
                return new ApiResponse("Error", _localizer["Error"].Value, null, false);
            }
        }

        public async Task<ApiResponse> SubmitQuizAttemptAsync(QuizSubmissionDTO submissionDTO, string studentId)
        {
            try
            {
                var attempt = await _context.QuizAttempts
                    .Include(qa => qa.Enrollment)
                    .Include(qa => qa.Quiz)
                        .ThenInclude(q => q.Questions)
                            .ThenInclude(qs => qs.QuestionOptions)
                    .FirstOrDefaultAsync(qa => qa.Id == submissionDTO.QuizAttemptId);

                if (attempt == null)
                    return new ApiResponse("NotFound", _localizer["AttemptNotFound"].Value, null, false);

                if (attempt.Enrollment.StudentId != studentId)
                    return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);

                if (attempt.CompletedAt.HasValue)
                    return new ApiResponse("Forbidden", _localizer["AlreadySubmitted"].Value, null, false);

                attempt.CompletedAt = DateTime.UtcNow;
                var correctAnswersCount = 0;
                var totalQuestions = attempt.Quiz.Questions.Count;

                foreach (var ansDto in submissionDTO.Answers)
                {
                    var question = attempt.Quiz.Questions.FirstOrDefault(q => q.Id == ansDto.QuestionId);
                    if (question == null) continue;

                    var selectedOption = question.QuestionOptions.FirstOrDefault(o => o.Id == ansDto.SelectedOptionId);
                    if (selectedOption == null) continue;

                    var answer = new QuizAttemptAnswer
                    {
                        Id = Guid.NewGuid().ToString(),
                        QuizAttemptId = attempt.Id,
                        QuestionId = question.Id,
                        SelectedOptionId = selectedOption.Id
                    };

                    if (selectedOption.IsCorrect)
                        correctAnswersCount++;

                    _context.QuizAttemptAnswers.Add(answer);
                }

                attempt.Score = totalQuestions > 0 ? (int)Math.Round((double)correctAnswersCount / totalQuestions * 100) : 0;

                await _context.SaveChangesAsync();

                return await GetQuizResultAsync(attempt.Id, studentId);
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Error submitting quiz: {ex.Message}");
                return new ApiResponse("Error", _localizer["Error"].Value, null, false);
            }
        }

        public async Task<ApiResponse> GetQuizResultAsync(string attemptId, string studentId)
        {
            try
            {
                var attempt = await _context.QuizAttempts
                    .Include(qa => qa.Enrollment)
                    .Include(qa => qa.Quiz)
                    .Include(qa => qa.QuizAttemptAnswers)
                        .ThenInclude(qaa => qaa.Question)
                            .ThenInclude(q => q.QuestionOptions)
                    .Include(qa => qa.QuizAttemptAnswers)
                        .ThenInclude(qaa => qaa.SelectedOption)
                    .FirstOrDefaultAsync(qa => qa.Id == attemptId);

                if (attempt == null)
                    return new ApiResponse("NotFound", _localizer["AttemptNotFound"].Value, null, false);

                if (attempt.Enrollment.StudentId != studentId)
                {
                     var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == attempt.Quiz.Lecture.CourseId);
                     if (course == null || course.InstructorId != studentId)
                         return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
                }

                if (!attempt.CompletedAt.HasValue)
                    return new ApiResponse("Forbidden", _localizer["AttemptNotCompleted"].Value, null, false);

                var resultDto = new QuizResultDTO
                {
                    QuizAttemptId = attempt.Id,
                    QuizId = attempt.QuizId,
                    QuizName = attempt.Quiz?.Name ?? "Deleted Quiz",
                    Score = attempt.Score,
                    TotalQuestions = attempt.QuizAttemptAnswers.Count,
                    CorrectAnswersCount = attempt.QuizAttemptAnswers.Count(qaa => qaa.SelectedOption?.IsCorrect ?? false),
                    AttemptedAt = attempt.AttemptedAt,
                    CompletedAt = attempt.CompletedAt,
                    DetailedResults = attempt.QuizAttemptAnswers.Select(qaa => {
                        var question = qaa.Question;
                        var selectedOption = qaa.SelectedOption;
                        var correctOption = question?.QuestionOptions.FirstOrDefault(o => o.IsCorrect);
                        
                        return new QuizAttemptAnswerResultDTO
                        {
                            QuestionId = qaa.QuestionId,
                            SelectedOptionId = qaa.SelectedOptionId,
                            CorrectOptionId = correctOption?.Id,
                            IsCorrect = selectedOption?.IsCorrect ?? false,
                            Explanation = question?.Explanation
                        };
                    }).ToList()
                };

                return new ApiResponse("Success", _localizer["Success"].Value, resultDto, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting quiz result: {ex.Message}");
                return new ApiResponse("Error", _localizer["Error"].Value, null, false);
            }
        }

        public async Task<ApiResponse> GetStudentQuizAttemptsAsync(string quizId, string studentId)
        {
            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Lectures)
                            .ThenInclude(l => l.Quizzes)
                    .FirstOrDefaultAsync(e => e.StudentId == studentId && e.Course.Lectures.Any(l => l.Quizzes.Any(q => q.Id == quizId)));

                if (enrollment == null)
                    return new ApiResponse("Forbidden", _localizer["NotEnrolled"].Value, null, false);

                var attempts = await _context.QuizAttempts
                    .Where(qa => qa.QuizId == quizId && qa.EnrollmentId == enrollment.Id)
                    .OrderByDescending(qa => qa.AttemptedAt)
                    .Select(qa => new QuizAttemptSummaryDTO
                    {
                        Id = qa.Id,
                        AttemptedAt = qa.AttemptedAt,
                        CompletedAt = qa.CompletedAt,
                        TotalQuestions = qa.Quiz.Questions.Count,
                        CorrectAnswersCount = qa.QuizAttemptAnswers.Count(qaa => qaa.SelectedOption.IsCorrect)
                    })
                    .ToListAsync();

                return new ApiResponse("Success", _localizer["Success"].Value, attempts, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting student quiz attempts: {ex.Message}");
                return new ApiResponse("Error", _localizer["Error"].Value, null, false);
            }
        }
    }
}


