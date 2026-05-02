using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CourseService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Shared.Application.Extension;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace CourseService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public WishlistController(IWishlistService wishlistService, IStringLocalizer<SharedResources> localizer)
        {
            _wishlistService = wishlistService;
            _localizer = localizer;
        }

        [HttpPost("add/{courseId}")]
        public async Task<ActionResult<ApiResponse>> AddToWishlist([FromRoute] string courseId)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _wishlistService.AddToWishlistAsync(courseId, studentId);
            return response.ToActionResult();
        }

        [HttpDelete("remove/{courseId}")]
        public async Task<ActionResult<ApiResponse>> RemoveFromWishlist([FromRoute] string courseId)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _wishlistService.RemoveFromWishlistAsync(courseId, studentId);
            return response.ToActionResult();
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetMyWishlist([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var studentId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new ApiResponse("Error", _localizer["Unauthorized"].Value, null, false));
            }
            var response = await _wishlistService.GetStudentWishlistAsync(studentId, pageNumber, pageSize);
            return response.ToActionResult();
        }
    }
}
