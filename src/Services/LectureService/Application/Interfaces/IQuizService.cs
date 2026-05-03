using System.Threading.Tasks;
using LectureService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace LectureService.Application.Interfaces
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
