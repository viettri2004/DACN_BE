using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.Interfaces;
using CourseService.Domain.Entities;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;

namespace CourseService.Infrastructure.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly AppDbContext _context;

        public CourseRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Course?> GetByIdAsync(string id)
        {
            return await _context.Courses
                .Include(c => c.CourseTags)
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Course>> GetAllAsync()
        {
            return await _context.Courses.ToListAsync();
        }

        public IQueryable<Course> GetQueryable()
        {
            return _context.Courses;
        }

        public async Task AddAsync(Course course)
        {
            await _context.Courses.AddAsync(course);
        }

        public async Task UpdateAsync(Course course)
        {
            _context.Courses.Update(course);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Course course)
        {
            _context.Courses.Remove(course);
            await Task.CompletedTask;
        }

        public async Task<List<Tag>> GetTagsByIdsAsync(List<string> tagIds)
        {
            return await _context.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();
        }

        public async Task<Enrollment?> GetEnrollmentAsync(string studentId, string courseId)
        {
            return await _context.Enrollments.FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        }

        public IQueryable<Enrollment> GetEnrollmentsQueryable()
        {
            return _context.Enrollments;
        }

        public async Task<Comment?> GetCommentByIdAsync(string id)
        {
            return await _context.Comments.FirstOrDefaultAsync(c => c.Id == id);
        }

        public IQueryable<Comment> GetCommentsQueryable()
        {
            return _context.Comments;
        }

        public async Task AddCommentAsync(Comment comment)
        {
            await _context.Comments.AddAsync(comment);
        }

        public async Task UpdateCommentAsync(Comment comment)
        {
            _context.Comments.Update(comment);
            await Task.CompletedTask;
        }

        public async Task DeleteCommentAsync(Comment comment)
        {
            _context.Comments.Remove(comment);
            await Task.CompletedTask;
        }

        public async Task<QAThread?> GetThreadByIdAsync(string id)
        {
            return await _context.QAThreads.FirstOrDefaultAsync(t => t.Id == id);
        }

        public IQueryable<QAThread> GetThreadsQueryable()
        {
            return _context.QAThreads;
        }

        public async Task AddThreadAsync(QAThread thread)
        {
            await _context.QAThreads.AddAsync(thread);
        }

        public async Task UpdateThreadAsync(QAThread thread)
        {
            _context.QAThreads.Update(thread);
            await Task.CompletedTask;
        }

        public async Task DeleteThreadAsync(QAThread thread)
        {
            _context.QAThreads.Remove(thread);
            await Task.CompletedTask;
        }

        public async Task<QAMessage?> GetMessageByIdAsync(string id)
        {
            return await _context.QAMessages.FirstOrDefaultAsync(m => m.Id == id);
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

        public async Task<StudentLectureProgress?> GetProgressAsync(string studentId, string lectureId, string itemId, string itemType)
        {
            return await _context.StudentLectureProgresses
                .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LectureId == lectureId && p.ItemId == itemId && p.ItemType == itemType);
        }

        public async Task AddProgressAsync(StudentLectureProgress progress)
        {
            await _context.StudentLectureProgresses.AddAsync(progress);
        }

        public async Task UpdateProgressAsync(StudentLectureProgress progress)
        {
            _context.StudentLectureProgresses.Update(progress);
            await Task.CompletedTask;
        }

        public async Task<Wishlist?> GetWishlistItemAsync(string studentId, string courseId)
        {
            return await _context.Wishlists.FirstOrDefaultAsync(w => w.StudentId == studentId && w.CourseId == courseId);
        }

        public async Task AddToWishlistAsync(Wishlist wishlist)
        {
            await _context.Wishlists.AddAsync(wishlist);
        }

        public async Task RemoveFromWishlistAsync(Wishlist wishlist)
        {
            _context.Wishlists.Remove(wishlist);
            await Task.CompletedTask;
        }

        public IQueryable<Wishlist> GetWishlistQueryable()
        {
            return _context.Wishlists;
        }

        public async Task<CourseRequest?> GetRequestByIdAsync(string id)
        {
            return await _context.CourseRequests.FirstOrDefaultAsync(r => r.Id == id);
        }

        public IQueryable<CourseRequest> GetRequestsQueryable()
        {
            return _context.CourseRequests;
        }

        public async Task AddRequestAsync(CourseRequest request)
        {
            await _context.CourseRequests.AddAsync(request);
        }

        public async Task UpdateRequestAsync(CourseRequest request)
        {
            _context.CourseRequests.Update(request);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
