using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using Entities;
using LectureService.Application.DTOs;
using LectureService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace LectureService.Infrastructure.Repositories
{
    public class QuizRepository : IQuizRepository
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public QuizRepository(AppDbContext context, IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _localizer = localizer;
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
                    AttemptCount = createQuizDTO.AttemptCount,
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

                if (updateQuizDTO.AttemptCount.HasValue)
                    quiz.AttemptCount = updateQuizDTO.AttemptCount.Value;

                if (updateQuizDTO.Questions != null)
                {
                    _context.Questions.RemoveRange(quiz.Questions);
                    
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

                await _context.SaveChangesAsync();

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
                    .FirstOrDefaultAsync(q => q.Id == quizId);

                if (quiz == null)
                    return new ApiResponse("NotFound", _localizer["QuizNotFound"].Value, null, false);

                if (quiz.Lecture.Course.InstructorId != instructorId)
                    return new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false);

                _context.Quizzes.Remove(quiz);
                await _context.SaveChangesAsync();

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
                    AttemptCount = quiz.AttemptCount,
                    Questions = quiz.Questions.Select(q => new QuestionDTO
                    {
                        Id = q.Id,
                        Content = q.Content,
                        DisplayOrder = q.DisplayOrder,
                        Explanation = q.Explanation,
                        Options = q.QuestionOptions.Select(o => new QuestionOptionDTO
                        {
                            Id = o.Id,
                            Content = o.Content,
                            IsCorrect = o.IsCorrect,
                            DisplayOrder = o.DisplayOrder
                        }).OrderBy(o => o.DisplayOrder).ToList()
                    }).OrderBy(q => q.DisplayOrder).ToList()
                };

                return new ApiResponse("Success", _localizer["Success"].Value, quizDto, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting quiz: {ex.Message}");
                return new ApiResponse("Error", _localizer["Error"].Value, null, false);
            }
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

                if (quiz.AttemptCount > 0)
                {
                    var attemptCount = await _context.QuizAttempts
                        .CountAsync(qa => qa.QuizId == quizId && qa.EnrollmentId == enrollment.Id);

                    if (attemptCount >= quiz.AttemptCount)
                        return new ApiResponse("Forbidden", _localizer["MaxAttemptsReached"].Value, null, false);
                }

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
                Console.WriteLine($"Error submitting quiz: {ex.Message}");
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
                        .ThenInclude(q => q.Questions)
                            .ThenInclude(qs => qs.QuestionOptions)
                    .Include(qa => qa.QuizAttemptAnswers)
                    .FirstOrDefaultAsync(qa => qa.Id == attemptId);

                if (attempt == null)
                    return new ApiResponse("NotFound", _localizer["AttemptNotFound"].Value, null, false);

                // Allow instructor of the course to see it as well?
                // For now just student
                if (attempt.Enrollment.StudentId != studentId)
                {
                     // Check if instructor
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
                    QuizName = attempt.Quiz.Name,
                    Score = attempt.Score,
                    TotalQuestions = attempt.Quiz.Questions.Count,
                    CorrectAnswersCount = attempt.QuizAttemptAnswers.Count(qaa => 
                        attempt.Quiz.Questions.First(q => q.Id == qaa.QuestionId)
                                .QuestionOptions.First(o => o.Id == qaa.SelectedOptionId).IsCorrect),
                    AttemptedAt = attempt.AttemptedAt,
                    CompletedAt = attempt.CompletedAt,
                    DetailedResults = attempt.QuizAttemptAnswers.Select(qaa => {
                        var question = attempt.Quiz.Questions.First(q => q.Id == qaa.QuestionId);
                        var correctOption = question.QuestionOptions.First(o => o.IsCorrect);
                        var selectedOption = question.QuestionOptions.First(o => o.Id == qaa.SelectedOptionId);
                        
                        return new QuizAttemptAnswerResultDTO
                        {
                            QuestionId = qaa.QuestionId,
                            SelectedOptionId = qaa.SelectedOptionId,
                            CorrectOptionId = correctOption.Id,
                            IsCorrect = selectedOption.IsCorrect,
                            Explanation = question.Explanation
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
                        Score = qa.Score
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