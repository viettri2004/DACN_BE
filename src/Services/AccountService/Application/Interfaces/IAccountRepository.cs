using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountService.Application.DTOs;
using Entities;
using src.Shared.Domain.Entities;

namespace AccountService.Application.Interfaces
{
    public interface IAccountRepository
    {
        Task<User> GetUserFromRefreshToken(string refreshToken);
        Task<User> FindUserByEmail(string email);
        Task ChangePassword(User user, ChangePasswordDTO changePasswordDTO);
        Task<ApiResponse> GetUserProfileAsync(string userId);
        Task<ApiResponse> UpdateUserProfileAsync(string userId, UpdateUserProfileDTO dto);
        Task<bool> CreateInstructorRequestAsync(InstructorRequest request);
        Task<List<InstructorRequest>> GetPendingInstructorRequestsAsync();
        Task<InstructorRequest?> GetInstructorRequestByIdAsync(int id);
        Task<InstructorRequest?> GetInstructorRequestByUserIdAsync(string userId);
        Task UpdateInstructorRequestAsync(InstructorRequest request);
        Task UpdateUserDiscriminatorToInstructor(string userId);
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetAllInstructorsAsync();
        Task<User?> GetUserByIdAsync(string userId);
        Task UpdateUserAsync(User user);
    }
}