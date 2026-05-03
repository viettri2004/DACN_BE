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
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [Authorize(Policy = "Admin")]
        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse>> GetDashboardStats()
        {
            var response = await _dashboardService.GetDashboardDataAsync();
            return response.ToActionResult();
        }
    }
}