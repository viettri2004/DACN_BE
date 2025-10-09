using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using src.Shared.Domain.Entities;

namespace StudentService.Application.Interfaces
{
    public interface IStudentRepository
    {
        Task<ApiResponse> GetRecommendedCoursesAsync();
        Task<ApiResponse> GetMyCoursesAsync(string studentId);
    }
}