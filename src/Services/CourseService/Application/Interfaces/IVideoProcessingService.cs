using System.Threading.Tasks;
using Hangfire;

namespace CourseService.Application.Interfaces
{
    public interface IVideoProcessingService
    {
        [Queue("video")]
        Task ProcessVideoAsync(string videoId);
    }
}
