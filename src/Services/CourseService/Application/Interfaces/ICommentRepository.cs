using System.Linq;
using System.Threading.Tasks;
using CourseService.Domain.Entities;
using Entities;

namespace CourseService.Application.Interfaces
{
    public interface ICommentRepository
    {
        Task<Comment?> GetByIdAsync(string id);
        IQueryable<Comment> GetCommentsQueryable();
        Task AddAsync(Comment comment);
        Task UpdateAsync(Comment comment);
        Task DeleteAsync(Comment comment);
        Task SaveChangesAsync();
    }
}
