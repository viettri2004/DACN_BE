using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.Interfaces;
using CourseService.Domain.Entities;
using Entities;
using Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CourseService.Infrastructure.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly AppDbContext _context;

        public CommentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Comment?> GetByIdAsync(string id)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public IQueryable<Comment> GetCommentsQueryable()
        {
            return _context.Comments;
        }

        public async Task AddAsync(Comment comment)
        {
            await _context.Comments.AddAsync(comment);
        }

        public async Task UpdateAsync(Comment comment)
        {
            _context.Comments.Update(comment);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Comment comment)
        {
            _context.Comments.Remove(comment);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
