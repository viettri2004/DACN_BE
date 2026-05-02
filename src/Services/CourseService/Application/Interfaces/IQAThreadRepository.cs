using System.Linq;
using System.Threading.Tasks;
using CourseService.Domain.Entities;
using Entities;

namespace CourseService.Application.Interfaces
{
    public interface IQAThreadRepository
    {
        Task<QAThread?> GetByIdAsync(string id);
        IQueryable<QAThread> GetThreadsQueryable();
        Task AddAsync(QAThread thread);
        Task UpdateAsync(QAThread thread);
        Task DeleteAsync(QAThread thread);

        Task<QAMessage?> GetMessageByIdAsync(string id);
        IQueryable<QAMessage> GetMessagesQueryable();
        Task AddMessageAsync(QAMessage message);
        Task UpdateMessageAsync(QAMessage message);
        Task DeleteMessageAsync(QAMessage message);

        Task SaveChangesAsync();
    }
}
