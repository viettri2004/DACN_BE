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
using ContentService.Application.Interfaces;
using System.Threading.Tasks;
using ContentService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace ContentService.Application.Interfaces
{
    public interface IQuizService
    {
        Task<ApiResponse> CreateQuizAsync(CreateQuizDTO createQuizDTO, string instructorId);
        Task<ApiResponse> UpdateQuizAsync(string quizId, UpdateQuizDTO updateQuizDTO, string instructorId);
        Task<ApiResponse> DeleteQuizAsync(string quizId, string instructorId);
        Task<ApiResponse> GetQuizByIdAsync(string quizId);

        // Quiz Attempt & Result
        Task<ApiResponse> StartQuizAttemptAsync(string quizId, string studentId);
        Task<ApiResponse> SubmitQuizAttemptAsync(QuizSubmissionDTO submissionDTO, string studentId);
        Task<ApiResponse> GetQuizResultAsync(string attemptId, string studentId);
        Task<ApiResponse> GetStudentQuizAttemptsAsync(string quizId, string studentId);
    }
}



