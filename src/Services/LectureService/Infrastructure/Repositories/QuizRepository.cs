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

        public async Task<ApiResponse> AddQuestionsToQuizAsync(UpdateQuizQuestionsDTO updateQuizQuestionsDTO, string instructorId)
        {
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Lecture)
                        .ThenInclude(l => l.Course)
                    .Include(q => q.Questionnaires)
                    .FirstOrDefaultAsync(q => q.Id == updateQuizQuestionsDTO.QuizId);

                if (quiz == null)
                    return new ApiResponse("NotFound", _localizer["QuizNotFound"].Value, null, false);

                if (quiz.Lecture.Course.InstructorId != instructorId)
                    return new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false);

                int nextQuestionNumber = 1;
                if (quiz.Questionnaires.Any())
                {
                    nextQuestionNumber = quiz.Questionnaires.Max(q => q.QuestionNumber) + 1;
                }

                if (updateQuizQuestionsDTO.Questions != null && updateQuizQuestionsDTO.Questions.Any())
                {
                    foreach (var q in updateQuizQuestionsDTO.Questions)
                    {
                        _context.Set<Questionnaire>().Add(new Questionnaire
                        {
                            QuizId = quiz.Id,
                            QuestionNumber = nextQuestionNumber++,
                            Question = q.Question,
                            Key = q.Key,
                            Description = q.Description
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["AddQuestionsSuccess"].Value, null, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding questions: {ex.Message}");
                return new ApiResponse("Error", _localizer["AddQuestionsFailed"].Value, null, false);
            }
        }
    }
}