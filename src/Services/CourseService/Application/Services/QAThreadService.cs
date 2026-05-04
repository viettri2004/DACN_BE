using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Domain.Entities;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace CourseService.Application.Services
{
    public class QAThreadService : IQAThreadService
    {
        private readonly IQAThreadRepository _qaRepository;
        private readonly ICourseRepository _courseRepository; // To check instructor status
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly Data.Context.AppDbContext _context;
        private readonly AccountService.Application.Interfaces.INotificationRepository _notificationRepository;

        public QAThreadService(IQAThreadRepository qaRepository, ICourseRepository courseRepository, IStringLocalizer<SharedResources> localizer, Data.Context.AppDbContext context, AccountService.Application.Interfaces.INotificationRepository notificationRepository)
        {
            _qaRepository = qaRepository;
            _courseRepository = courseRepository;
            _localizer = localizer;
            _context = context;
            _notificationRepository = notificationRepository;
        }

        public async Task<ApiResponse> GetCourseQAThreadsAsync(string courseId, string userId, int pageNumber, int pageSize, string filter = "all")
        {
            var query = _qaRepository.GetThreadsQueryable()
                .AsNoTracking()
                .Where(t => t.CourseId == courseId);

            if (filter == "read")
            {
                var course = await _courseRepository.GetByIdAsync(courseId);
                string instructorId = course?.InstructorId ?? string.Empty;
                query = query.Where(t => t.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.UserId).FirstOrDefault() == instructorId);
            }
            else if (filter == "unread")
            {
                var course = await _courseRepository.GetByIdAsync(courseId);
                string instructorId = course?.InstructorId ?? string.Empty;
                query = query.Where(t => t.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.UserId).FirstOrDefault() != instructorId);
            }

            var totalCount = await query.CountAsync();
            var threads = await query
                .Include(t => t.Creator)
                .Include(t => t.Messages)
                .OrderByDescending(t => t.LastActivityAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = threads.Select(t => new QAThreadDTO
            {
                Id = t.Id,
                Title = t.Title,
                CreatorName = t.Creator.FullName,
                CreatorAvatarUrl = t.Creator.AvatarUrl,
                CreatedAt = t.CreatedAt,
                LastActivityAt = t.LastActivityAt,
                IsMyThread = t.CreatorId == userId,
                TotalMessages = t.Messages.Count
            }).ToList();

            return new ApiResponse("Success", _localizer["Success"].Value, new PagedResult<QAThreadDTO> { Items = result, Page = pageNumber, PageSize = pageSize, TotalCount = totalCount }, true);
        }

        public async Task<ApiResponse> GetThreadMessagesAsync(string threadId, string userId, int pageNumber, int pageSize)
        {
            var thread = await _qaRepository.GetByIdAsync(threadId);
            if (thread == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            var course = await _courseRepository.GetByIdAsync(thread.CourseId);
            string instructorId = course?.InstructorId ?? string.Empty;

            var query = _qaRepository.GetMessagesQueryable()
                .AsNoTracking()
                .Where(m => m.ThreadId == threadId);

            var totalCount = await query.CountAsync();
            var messages = await query
                .Include(m => m.User)
                .OrderBy(m => m.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = messages.Select(m => new QAMessageDTO
            {
                Id = m.Id,
                Content = m.Content,
                UserName = m.User.FullName,
                AvatarUrl = m.User.AvatarUrl,
                CreatedAt = m.CreatedAt,
                IsMyMessage = m.UserId == userId,
                IsInstructor = m.UserId == instructorId
            }).ToList();

            return new ApiResponse("Success", _localizer["Success"].Value, new { 
                thread = new QAThreadDetailDTO { 
                    Id = thread.Id, 
                    Title = thread.Title,
                    CreatorName = thread.Creator.FullName,
                    CreatorAvatarUrl = thread.Creator.AvatarUrl,
                    CreatedAt = thread.CreatedAt,
                    IsMyThread = thread.CreatorId == userId
                }, 
                messages = new PagedResult<QAMessageDTO> { Items = result, Page = pageNumber, PageSize = pageSize, TotalCount = totalCount } 
            }, true);
        }

        public async Task<ApiResponse> CreateQAThreadAsync(CreateThreadDTO createThreadDTO, string userId)
        {
            var thread = new QAThread
            {
                Id = Guid.NewGuid().ToString(),
                CourseId = createThreadDTO.CourseId,
                CreatorId = userId,
                Title = createThreadDTO.Title,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            };

            await _qaRepository.AddAsync(thread);
            await _qaRepository.SaveChangesAsync();

            var course = await _courseRepository.GetByIdAsync(createThreadDTO.CourseId);
            if (course != null)
            {
                var creator = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                var creatorName = creator?.FullName ?? "Học viên";
                var title = _localizer["NewQAQuestionNotifTitle"].Value;
                var message = string.Format(_localizer["NewQAQuestionNotifMessage"].Value, creatorName, course.Name, createThreadDTO.Title);

                await _notificationRepository.CreateNotificationAsync(new Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = course.InstructorId,
                    Title = title,
                    Message = message,
                    Type = AccountService.Domain.Enums.NotificationType.NewQAQuestion,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            return new ApiResponse("Created", _localizer["Success"].Value, thread.Id, true);
        }

        public async Task<ApiResponse> AddMessageToThreadAsync(AddMessageDTO addMessageDTO, string userId)
        {
            var thread = await _qaRepository.GetByIdAsync(addMessageDTO.ThreadId);
            if (thread == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            var message = new QAMessage
            {
                Id = Guid.NewGuid().ToString(),
                ThreadId = thread.Id,
                UserId = userId,
                Content = addMessageDTO.Content,
                CreatedAt = DateTime.UtcNow
            };

            thread.LastActivityAt = DateTime.UtcNow;
            await _qaRepository.AddMessageAsync(message);
            await _qaRepository.SaveChangesAsync();

            try
            {
                var course = await _courseRepository.GetByIdAsync(thread.CourseId);
                if (course != null)
                {
                    var replier = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    var replierName = replier?.FullName ?? "Người dùng";

                    // 1. Notify the instructor if the replier is NOT the instructor
                    if (userId != course.InstructorId)
                    {
                        var title = _localizer["NewQAReplyNotifTitle"].Value;
                        var msg = string.Format(_localizer["NewQAReplyNotifMessage"].Value, replierName, course.Name, thread.Title);

                        await _notificationRepository.CreateNotificationAsync(new Notification
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserId = course.InstructorId,
                            Title = title,
                            Message = msg,
                            Type = AccountService.Domain.Enums.NotificationType.NewQAQuestion,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        });
                    }

                    // 2. Notify the original thread creator if the replier is NOT the creator
                    if (userId != thread.CreatorId)
                    {
                        var title = _localizer["NewQAReplyNotifTitle"].Value;
                        var msg = string.Format(_localizer["NewQAReplyForCreatorNotifMessage"].Value, replierName, thread.Title);

                        await _notificationRepository.CreateNotificationAsync(new Notification
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserId = thread.CreatorId,
                            Title = title,
                            Message = msg,
                            Type = AccountService.Domain.Enums.NotificationType.NewQAQuestion,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending thread reply notifications: {ex.Message}");
            }

            return new ApiResponse("Created", _localizer["Success"].Value, message.Id, true);
        }

        public async Task<ApiResponse> UpdateQAThreadAsync(string threadId, UpdateThreadDTO updateThreadDTO, string userId)
        {
            var thread = await _qaRepository.GetByIdAsync(threadId);
            if (thread == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            if (thread.CreatorId != userId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);

            thread.Title = updateThreadDTO.Title;
            await _qaRepository.UpdateAsync(thread);
            await _qaRepository.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> UpdateQAMessageAsync(string messageId, UpdateMessageDTO updateMessageDTO, string userId)
        {
            var message = await _qaRepository.GetMessageByIdAsync(messageId);
            if (message == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            if (message.UserId != userId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);

            message.Content = updateMessageDTO.Content;
            message.UpdatedAt = DateTime.UtcNow;

            await _qaRepository.UpdateMessageAsync(message);
            await _qaRepository.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> DeleteQAThreadAsync(string threadId, string userId)
        {
            var thread = await _qaRepository.GetByIdAsync(threadId);
            if (thread == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            
            // Check if user is creator OR instructor of the course
            var course = await _courseRepository.GetByIdAsync(thread.CourseId);
            if (thread.CreatorId != userId && (course == null || course.InstructorId != userId))
                return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);

            await _qaRepository.DeleteAsync(thread);
            await _qaRepository.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> DeleteQAMessageAsync(string messageId, string userId)
        {
            var message = await _qaRepository.GetMessageByIdAsync(messageId);
            if (message == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            
            if (message.UserId != userId)
            {
                var thread = await _qaRepository.GetByIdAsync(message.ThreadId);
                var course = await _courseRepository.GetByIdAsync(thread.CourseId);
                if (course == null || course.InstructorId != userId)
                    return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            }

            await _qaRepository.DeleteMessageAsync(message);
            await _qaRepository.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }
    }
}
