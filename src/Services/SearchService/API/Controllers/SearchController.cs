using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchService.Application.Interfaces;
using src.Shared.Domain.Entities;
using Microsoft.Extensions.Localization;
using src.Shared.Resources;
using Shared.Application.Extension;

namespace SearchService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ILuceneSearchService _searchService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public SearchController(ILuceneSearchService searchService, IStringLocalizer<SharedResources> localizer)
        {
            _searchService = searchService;
            _localizer = localizer;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("index-all")]
        public async Task<IActionResult> IndexAll()
        {
            await _searchService.IndexAllCoursesAsync();
            return Ok(new ApiResponse("Success", _localizer["ReIndexStarted"].Value, null, true));
        }
    }
}
