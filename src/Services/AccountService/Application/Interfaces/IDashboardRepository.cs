using System.Threading.Tasks;
using AccountService.Application.DTOs;
using src.Shared.Domain.Entities;

namespace AccountService.Application.Interfaces
{
    public interface IDashboardRepository
    {
        Task<ApiResponse> GetDashboardDataAsync();
        Task<ApiResponse> GetAdminNotificationsAsync();
    }
}