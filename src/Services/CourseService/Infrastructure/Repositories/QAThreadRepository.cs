using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.Interfaces;
using CourseService.Domain.Entities;
using Entities;
using Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CourseService.Infrastructure.Repositories
{
    public class QAThreadRepository : IQAThreadRepository
    {
        private readonly AppDbContext _context;

        public QAThreadRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<QAThread?> GetByIdAsync(string id)
        {
            return await _context.QAThreads
                .Include(t => t.Creator)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public IQueryable<QAThread> GetThreadsQueryable()
        {
            return _context.QAThreads;
        }

        public async Task AddAsync(QAThread thread)
        {
            await _context.QAThreads.AddAsync(thread);
        }

        public async Task UpdateAsync(QAThread thread)
        {
            _context.QAThreads.Update(thread);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(QAThread thread)
        {
            _context.QAThreads.Remove(thread);
            await Task.CompletedTask;
        }

        public async Task<QAMessage?> GetMessageByIdAsync(string id)
        {
            return await _context.QAMessages
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public IQueryable<QAMessage> GetMessagesQueryable()
        {
            return _context.QAMessages;
        }

        public async Task AddMessageAsync(QAMessage message)
        {
            await _context.QAMessages.AddAsync(message);
        }

        public async Task UpdateMessageAsync(QAMessage message)
        {
            _context.QAMessages.Update(message);
            await Task.CompletedTask;
        }

        public async Task DeleteMessageAsync(QAMessage message)
        {
            _context.QAMessages.Remove(message);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
