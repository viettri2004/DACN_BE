using System.Threading.Tasks;
using AccountService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;

namespace AccountService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardController(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        [Authorize(Policy = "Admin")]
        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse>> GetDashboardStats()
        {
            var response = await _dashboardRepository.GetDashboardDataAsync();
            return response.ToActionResult();
        }
    }
}