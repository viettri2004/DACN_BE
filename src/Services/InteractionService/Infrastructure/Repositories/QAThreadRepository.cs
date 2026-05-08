using SearchService.Application.DTOs;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Entities;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using OrderingService.Domain.Entities;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using LearningService.Application.Services;
using LearningService.Application.Interfaces;
using LearningService.Domain.Entities;
using InteractionService.Application.DTOs;
using InteractionService.Domain.Enums;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Entities;
using System.Linq;
using System.Threading.Tasks;
using InteractionService.Application.Interfaces;
using InteractionService.Domain.Entities;
using Data.Context;
using Microsoft.EntityFrameworkCore;

namespace InteractionService.Infrastructure.Repositories
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


