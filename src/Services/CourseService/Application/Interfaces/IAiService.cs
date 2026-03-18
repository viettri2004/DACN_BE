using CourseService.Application.DTOs;
using System.Threading.Tasks;

namespace CourseService.Application.Interfaces
{
    public interface IAiService
    {
        Task<LmsAnalysisResponse> ProcessVideo(string cloudinaryUrl);
    }
}
