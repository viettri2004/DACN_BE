using System.Threading.Tasks;

namespace CourseService.Application.Interfaces
{
    public interface IVideoProcessingService
    {
        Task ProcessVideoAsync(string videoId);
    }
}
