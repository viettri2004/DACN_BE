using System.Threading.Tasks;
using src.Shared.Domain.Entities;

namespace AccountService.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<ApiResponse> GetDashboardDataAsync();
    }
}
