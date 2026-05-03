using System.Threading.Tasks;
using AccountService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace AccountService.Application.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse> GetUserProfileAsync(string userId);
        Task<ApiResponse> UpdateUserProfileAsync(string userId, UpdateUserProfileDTO dto);
        Task<ApiResponse> GetPendingInstructorRequestsAsync();
        Task<ApiResponse> ApproveInstructorRequestAsync(int requestId);
        Task<ApiResponse> RejectInstructorRequestAsync(int requestId, string reason);
    }
}
