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
using InteractionService.Application.Interfaces;
using InteractionService.Domain.Enums;
using InteractionService.Domain.Entities;
using ContentService.Application.DTOs;
using ContentService.Domain.Enums;
using ContentService.Application.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentService.Domain.Entities;

namespace ContentService.Application.Interfaces
{
    public interface ICourseRepository
    {
        Task<Course?> GetByIdAsync(string id);
        Task<List<Course>> GetAllAsync();
        IQueryable<Course> GetQueryable();
        Task AddAsync(Course course);
        Task UpdateAsync(Course course);
        Task DeleteAsync(Course course);

        // Tags
        Task<List<Tag>> GetTagsByIdsAsync(List<string> tagIds);
        
        // Enrollments
        Task<Enrollment?> GetEnrollmentAsync(string studentId, string courseId);
        IQueryable<Enrollment> GetEnrollmentsQueryable();

        // Comments
        Task<Comment?> GetCommentByIdAsync(string id);
        IQueryable<Comment> GetCommentsQueryable();
        Task AddCommentAsync(Comment comment);
        Task UpdateCommentAsync(Comment comment);
        Task DeleteCommentAsync(Comment comment);

        // QA
        Task<QAThread?> GetThreadByIdAsync(string id);
        IQueryable<QAThread> GetThreadsQueryable();
        Task AddThreadAsync(QAThread thread);
        Task UpdateThreadAsync(QAThread thread);
        Task DeleteThreadAsync(QAThread thread);

        Task<QAMessage?> GetMessageByIdAsync(string id);
        IQueryable<QAMessage> GetMessagesQueryable();
        Task AddMessageAsync(QAMessage message);
        Task UpdateMessageAsync(QAMessage message);
        Task DeleteMessageAsync(QAMessage message);

        // Progress
        Task<StudentLectureProgress?> GetProgressAsync(string studentId, string lectureId, string itemId, string itemType);
        Task AddProgressAsync(StudentLectureProgress progress);
        Task UpdateProgressAsync(StudentLectureProgress progress);
        IQueryable<StudentLectureProgress> GetProgressQueryable();

        // Wishlist
        Task<Wishlist?> GetWishlistItemAsync(string studentId, string courseId);
        Task AddToWishlistAsync(Wishlist wishlist);
        Task RemoveFromWishlistAsync(Wishlist wishlist);
        IQueryable<Wishlist> GetWishlistQueryable();

        // Requests
        Task<CourseRequest?> GetRequestByIdAsync(string id);
        IQueryable<CourseRequest> GetRequestsQueryable();
        Task AddRequestAsync(CourseRequest request);
        Task UpdateRequestAsync(CourseRequest request);

        Task SaveChangesAsync();
    }
}



