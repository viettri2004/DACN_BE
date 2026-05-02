using System.Threading.Tasks;
using src.Shared.Domain.Entities;

namespace CourseService.Application.Interfaces
{
    public interface IWishlistService
    {
        Task<ApiResponse> AddToWishlistAsync(string courseId, string studentId);
        Task<ApiResponse> RemoveFromWishlistAsync(string courseId, string studentId);
        Task<ApiResponse> GetStudentWishlistAsync(string studentId, int pageNumber, int pageSize);
    }
}
