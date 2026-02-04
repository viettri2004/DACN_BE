using System.Threading.Tasks;
using LectureService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace LectureService.Application.Interfaces
{
    public interface IQuizRepository
    {
        Task<ApiResponse> CreateQuizAsync(CreateQuizDTO createQuizDTO, string instructorId);
        Task<ApiResponse> AddQuestionsToQuizAsync(UpdateQuizQuestionsDTO updateQuizQuestionsDTO, string instructorId);
    }
}