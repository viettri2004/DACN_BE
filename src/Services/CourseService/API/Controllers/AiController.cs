using Microsoft.AspNetCore.Mvc;
using CourseService.Application.Interfaces;
using CourseService.Application.DTOs;

namespace CourseService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;

        public AiController(IAiService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<LmsAnalysisResponse>> AnalyzeVideo([FromBody] string cloudinaryUrl)
        {
            if (string.IsNullOrEmpty(cloudinaryUrl))
                return BadRequest("Cloudinary URL is required.");

            try
            {
                var result = await _aiService.ProcessVideo(cloudinaryUrl);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
