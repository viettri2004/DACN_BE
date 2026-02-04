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
                    Questionnaires = new List<Questionnaire>()
                };

                if (createQuizDTO.Questions != null && createQuizDTO.Questions.Any())
                {
                    int questionNumber = 1;
                    foreach (var q in createQuizDTO.Questions)
                    {
                        quiz.Questionnaires.Add(new Questionnaire
                        {
                            QuizId = quiz.Id,
                            QuestionNumber = questionNumber++,
                            Question = q.Question,
                            Key = q.Key,
                            Description = q.Description
                        });
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
                    .Include(q => q.Questionnaires)
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
                    // Remove existing questions
                    _context.Set<Questionnaire>().RemoveRange(quiz.Questionnaires);
                    
                    // Add new questions
                    int questionNumber = 1;
                    foreach (var q in updateQuizDTO.Questions)
                    {
                        _context.Set<Questionnaire>().Add(new Questionnaire
                        {
                            QuizId = quiz.Id,
                            QuestionNumber = questionNumber++,
                            Question = q.Question,
                            Key = q.Key,
                            Description = q.Description
                        });
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
    }
}