using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Domain.Entities;
using CourseService.Domain.Enums;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Shared.Domain.Entities;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using AccountService.Application.Interfaces;
using AccountService.Domain.Enums;
using Hangfire;

namespace CourseService.Application.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILuceneSearchService _luceneSearchService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IDistributedCache _cache;

        public CourseService(ICourseRepository courseRepository,
                            IStringLocalizer<SharedResources> localizer,
                            ILuceneSearchService luceneSearchService,
                            INotificationRepository notificationRepository,
                            IBackgroundJobClient backgroundJobClient,
                            IDistributedCache cache)
        {
            _courseRepository = courseRepository;
            _localizer = localizer;
            _luceneSearchService = luceneSearchService;
            _notificationRepository = notificationRepository;
            _backgroundJobClient = backgroundJobClient;
            _cache = cache;
        }

        #region Course Management

        public async Task<ApiResponse> CreateCourseAsync(CreateCourseDTO createCourseDTO, string instructorId)
        {
            try
            {
                var newCourse = new Course
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = createCourseDTO.name,
                    Price = createCourseDTO.price,
                    Description = createCourseDTO.description ?? string.Empty,
                    ImageUrl = createCourseDTO.imageUrl ?? string.Empty,
                    ImagePublicId = createCourseDTO.imagePublicId ?? string.Empty,
                    InstructorId = instructorId,
                    CreateTime = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = CourseStatus.Private
                };

                if (createCourseDTO.TagIds != null && createCourseDTO.TagIds.Any())
                {
                    var tags = await _courseRepository.GetTagsByIdsAsync(createCourseDTO.TagIds);
                    foreach (var tag in tags)
                    {
                        newCourse.CourseTags.Add(new CourseTag { CourseId = newCourse.Id, TagId = tag.Id });
                    }
                }

                await _courseRepository.AddAsync(newCourse);
                await _courseRepository.SaveChangesAsync();
                await RemoveRecommendedCache();

                _backgroundJobClient.Enqueue(() => _luceneSearchService.IndexCourseAsync(newCourse));

                return new ApiResponse("Created", _localizer["CreateCourseSuccess"].Value, new InstructorCourseListDTO
                {
                    Id = newCourse.Id,
                    Name = newCourse.Name,
                    ImageUrl = newCourse.ImageUrl,
                    Price = newCourse.Price,
                    TotalStudents = 0,
                    Rating = 0,
                    Status = newCourse.Status.ToString()
                }, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", _localizer["CourseCreationFailed"].Value, ex.Message, false);
            }
        }

        public async Task<ApiResponse> UpdateCourseAsync(string courseId, UpdateCourseDTO updateCourseDTO, string instructorId)
        {
            try
            {
                var course = await _courseRepository.GetByIdAsync(courseId);
                if (course == null) return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);
                if (course.InstructorId != instructorId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);

                course.Name = updateCourseDTO.name;
                course.Price = updateCourseDTO.price;
                course.Description = updateCourseDTO.description ?? course.Description;
                course.ImageUrl = updateCourseDTO.imageUrl ?? course.ImageUrl;
                course.ImagePublicId = updateCourseDTO.imagePublicId ?? course.ImagePublicId;
                course.UpdatedAt = DateTime.UtcNow;

                if (updateCourseDTO.TagIds != null)
                {
                    course.CourseTags.Clear();
                    var tags = await _courseRepository.GetTagsByIdsAsync(updateCourseDTO.TagIds);
                    foreach (var tag in tags)
                    {
                        course.CourseTags.Add(new CourseTag { CourseId = course.Id, TagId = tag.Id });
                    }
                }

                await _courseRepository.UpdateAsync(course);
                await _courseRepository.SaveChangesAsync();
                await RemoveRecommendedCache();
                _backgroundJobClient.Enqueue(() => _luceneSearchService.IndexCourseAsync(course));

                return new ApiResponse("Success", _localizer["UpdateCourseSuccess"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> DeleteCourseAsync(string courseId, string instructorId)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null) return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);
            if (course.InstructorId != instructorId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);

            await _courseRepository.DeleteAsync(course);
            await _courseRepository.SaveChangesAsync();
            await RemoveRecommendedCache();
            _backgroundJobClient.Enqueue(() => _luceneSearchService.DeleteCourseFromIndexAsync(courseId));

            return new ApiResponse("Success", _localizer["DeleteCourseSuccess"].Value, null, true);
        }

        public async Task<ApiResponse> GetCourseDetailAsync(string courseId, string studentId)
        {
            var course = await _courseRepository.GetQueryable()
                .AsNoTracking()
                .Include(c => c.Instructor)
                .Include(c => c.CourseTags).ThenInclude(ct => ct.Tag)
                .Include(c => c.Lectures.OrderBy(l => l.DisplayOrder))
                    .ThenInclude(l => l.LectureVideos.OrderBy(v => v.DisplayOrder))
                .Include(c => c.Enrollments).ThenInclude(e => e.Comments)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);

            bool isEnrolled = !string.IsNullOrEmpty(studentId) && 
                await _courseRepository.GetEnrollmentAsync(studentId, courseId) != null;

            var totalLessons = course.Lectures.Sum(l => l.LectureVideos.Count + (l.Documents?.Count ?? 0) + (l.Quizzes?.Count ?? 0));
            var totalDuration = course.Lectures.SelectMany(l => l.LectureVideos).Sum(v => v.Duration);

            var response = new CourseDetailDTO
            {
                Id = course.Id,
                Name = course.Name,
                Description = course.Description,
                ImageUrl = course.ImageUrl,
                Price = course.Price,
                InstructorName = course.Instructor.FullName,
                InstructorAvatar = course.Instructor.AvatarUrl,
                InstructorIntro = course.Instructor.Description,
                UpdatedAt = course.UpdatedAt,
                IsEnrolled = isEnrolled,
                TotalLessons = totalLessons,
                TotalHours = Math.Round(totalDuration / 3600.0, 1),
                Tags = course.CourseTags.Select(ct => ct.Tag.Name).ToList(),
                Rating = course.Enrollments.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).Any() 
                         ? course.Enrollments.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).Average(cm => cm.Rate) 
                         : 0,
                TotalReviews = course.Enrollments.SelectMany(e => e.Comments).Count(cm => cm.Type == CommentType.Review),
                Curriculum = course.Lectures.Select(l => new CurriculumSectionDTO
                {
                    Id = l.Id,
                    Title = l.Name,
                    Lessons = l.LectureVideos.Select(v => new CurriculumLessonDTO
                    {
                        Id = v.Id,
                        Title = v.Name,
                        Duration = $"{v.Duration / 60:00}:{v.Duration % 60:00}",
                        Type = "video",
                        IsPreview = false
                    }).ToList()
                }).ToList()
            };

            return new ApiResponse("Success", _localizer["Success"].Value, response, true);
        }

        public async Task<ApiResponse> GetRecommendedCoursesAsync(string? userId, int pageNumber, int pageSize)
        {
            string cacheKey = string.IsNullOrEmpty(userId) ? $"course:recommended:guest:p{pageNumber}:s{pageSize}" : $"course:recommended:user:{userId}:p{pageNumber}:s{pageSize}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<ApiResponse>(cachedData)!;
            }

            var enrolledCourseIds = new List<string>();
            var enrolledTags = new List<string>();

            if (!string.IsNullOrEmpty(userId))
            {
                var userEnrollments = await _courseRepository.GetEnrollmentsQueryable()
                    .AsNoTracking()
                    .Include(e => e.Course).ThenInclude(c => c.CourseTags)
                    .Where(e => e.StudentId == userId && e.Status == true)
                    .ToListAsync();

                enrolledCourseIds = userEnrollments.Select(e => e.CourseId).ToList();
                enrolledTags = userEnrollments.SelectMany(e => e.Course.CourseTags).Select(ct => ct.TagId).Distinct().ToList();
            }

            IQueryable<Course> query = _courseRepository.GetQueryable()
                .AsNoTracking()
                .Include(c => c.Instructor)
                .Include(c => c.Enrollments).ThenInclude(e => e.Comments)
                .Where(c => c.Status == CourseStatus.Public && !enrolledCourseIds.Contains(c.Id));

            if (enrolledTags.Any())
            {
                query = query.Where(c => c.CourseTags.Any(ct => enrolledTags.Contains(ct.TagId)));
            }

            var totalCount = await query.CountAsync();
            var courses = await query
                .OrderByDescending(c => c.Enrollments.Count)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CourseListDTO
                {
                    Id = c.Id,
                    ImageUrl = c.ImageUrl,
                    Name = c.Name,
                    InstructorName = c.Instructor.FullName,
                    Rating = c.Enrollments.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).Any() 
                             ? c.Enrollments.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).Average(cm => cm.Rate) 
                             : 0,
                    Price = c.Price,
                    TotalStudents = c.Enrollments.Count
                })
                .ToListAsync();

            if (courses.Count == 0 && enrolledTags.Any() && pageNumber == 1)
            {
                return await GetRecommendedCoursesAsync(null, 1, pageSize); 
            }

            var pagedResult = new PagedResult<CourseListDTO> { Items = courses, Page = pageNumber, PageSize = pageSize, TotalCount = totalCount };
            var response = new ApiResponse("Success", _localizer["Success"].Value, pagedResult, true);

            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) });
            return response;
        }

        public async Task<ApiResponse> GetCourseCommentsAsync(string courseId, string? userId, CommentType type, int pageNumber, int pageSize, int? rating = null)
        {
            var query = _courseRepository.GetCommentsQueryable()
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Replies).ThenInclude(r => r.User)
                .Where(c => c.CourseId == courseId && c.Type == type && c.ReplyId == null);

            if (rating.HasValue) query = query.Where(c => c.Rate == rating.Value);

            var totalCount = await query.CountAsync();
            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CommentDTO
                {
                    CommentId = c.Id,
                    UserName = c.User.FullName,
                    AvatarUrl = c.User.AvatarUrl,
                    Content = c.Content,
                    Rate = c.Rate,
                    Timestamp = c.CreatedAt,
                    IsMyComment = userId != null && c.UserId == userId,
                    Replies = c.Replies.Select(r => new ReplyDTO
                    {
                        CommentId = r.Id,
                        Content = r.Content,
                        Timestamp = r.CreatedAt,
                        IsMyComment = userId != null && r.UserId == userId
                    }).ToList()
                }).ToListAsync();

            var pagedResult = new PagedResult<CommentDTO> { Items = comments, Page = pageNumber, PageSize = pageSize, TotalCount = totalCount };
            return new ApiResponse("Success", _localizer["Success"].Value, pagedResult, true);
        }

        #endregion

        #region Student Features

        public async Task<ApiResponse> GetCoursesByStudentIdAsync(string studentId, int pageNumber, int pageSize)
        {
            var query = _courseRepository.GetEnrollmentsQueryable()
                .AsNoTracking()
                .Include(e => e.Course).ThenInclude(c => c.Instructor)
                .Include(e => e.Course).ThenInclude(c => c.Lectures)
                .Where(e => e.StudentId == studentId && e.Status == true);

            var totalCount = await query.CountAsync();
            var enrollments = await query
                .OrderByDescending(e => e.EnrolledAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var courseList = new List<MyCourseDTO>();
            foreach (var e in enrollments)
            {
                var totalLessons = e.Course.Lectures.Sum(l => (l.LectureVideos?.Count ?? 0) + (l.Documents?.Count ?? 0) + (l.Quizzes?.Count ?? 0));
                courseList.Add(new MyCourseDTO
                {
                    Id = e.Course.Id,
                    Name = e.Course.Name,
                    ImageUrl = e.Course.ImageUrl,
                    InstructorName = e.Course.Instructor.FullName,
                    EnrolledAt = e.EnrolledAt,
                    Progress = 0,
                    TotalLessons = totalLessons,
                    CompletedLessons = 0
                });
            }

            return new ApiResponse("Success", _localizer["Success"].Value, new PagedResult<MyCourseDTO> { Items = courseList, Page = pageNumber, PageSize = pageSize, TotalCount = totalCount }, true);
        }

        public async Task<ApiResponse> GetCourseContentAsync(string courseId, string userId)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            var enrollment = await _courseRepository.GetEnrollmentAsync(userId, courseId);
            if (enrollment == null) return new ApiResponse("Forbidden", _localizer["NotEnrolled"].Value, null, false);

            var fullCourse = await _courseRepository.GetQueryable()
                .AsNoTracking()
                .Include(c => c.Lectures.OrderBy(l => l.DisplayOrder))
                    .ThenInclude(l => l.LectureVideos.OrderBy(v => v.DisplayOrder))
                .Include(c => c.Lectures).ThenInclude(l => l.Documents)
                .Include(c => c.Lectures).ThenInclude(l => l.Quizzes)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            var response = new CourseContentDTO
            {
                Id = fullCourse.Id,
                Name = fullCourse.Name,
                Lectures = fullCourse.Lectures.Select(l => new LectureContentDTO
                {
                    Id = l.Id,
                    Name = l.Name,
                    Videos = l.LectureVideos.Select(v => new VideoContentDTO { Id = v.Id, Name = v.Name, Duration = v.Duration, DisplayOrder = v.DisplayOrder }).ToList(),
                    Documents = l.Documents.Select(d => new DocumentContentDTO { Id = d.Id, Name = d.Name }).ToList(),
                    Quizzes = l.Quizzes.Select(q => new QuizContentDTO { Id = q.Id, Name = q.Name }).ToList()
                }).ToList()
            };

            return new ApiResponse("Success", _localizer["Success"].Value, response, true);
        }

        public async Task<ApiResponse> AddCommentAsync(AddCommentDTO addCommentDTO, string userId)
        {
            var enrollment = await _courseRepository.GetEnrollmentAsync(userId, addCommentDTO.CourseId);
            if (enrollment == null) return new ApiResponse("Forbidden", _localizer["NotEnrolledInCourse"].Value, null, false);

            var comment = new Comment
            {
                Id = Guid.NewGuid().ToString(),
                Content = addCommentDTO.Content,
                Rate = addCommentDTO.Type == CommentType.Review ? addCommentDTO.Rate : 0,
                EnrollmentId = enrollment.Id,
                UserId = userId,
                CourseId = addCommentDTO.CourseId,
                CreatedAt = DateTime.UtcNow,
                Type = addCommentDTO.Type
            };

            await _courseRepository.AddCommentAsync(comment);
            await _courseRepository.SaveChangesAsync();

            return new ApiResponse("Created", _localizer["Success"].Value, comment.Id, true);
        }

        public async Task<ApiResponse> UpdateCommentAsync(string commentId, UpdateCommentDTO updateCommentDTO, string userId)
        {
            var comment = await _courseRepository.GetCommentByIdAsync(commentId);
            if (comment == null) return new ApiResponse("NotFound", _localizer["CommentNotFound"].Value, null, false);
            if (comment.UserId != userId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);

            comment.Content = updateCommentDTO.Content;
            if (comment.Type == CommentType.Review) comment.Rate = updateCommentDTO.Rate;
            comment.UpdatedAt = DateTime.UtcNow;

            await _courseRepository.UpdateCommentAsync(comment);
            await _courseRepository.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["CommentUpdated"].Value, null, true);
        }

        public async Task<ApiResponse> DeleteCommentAsync(string commentId, string userId)
        {
            var comment = await _courseRepository.GetCommentByIdAsync(commentId);
            if (comment == null) return new ApiResponse("NotFound", _localizer["CommentNotFound"].Value, null, false);
            if (comment.UserId != userId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);

            await _courseRepository.DeleteCommentAsync(comment);
            await _courseRepository.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["CommentDeleted"].Value, null, true);
        }

        public async Task<ApiResponse> ReplyToCommentAsync(AddReplyCommentDTO replyDTO, string userId)
        {
            var parent = await _courseRepository.GetCommentByIdAsync(replyDTO.ParentCommentId);
            if (parent == null) return new ApiResponse("NotFound", _localizer["ParentCommentNotFound"].Value, null, false);

            var reply = new Comment
            {
                Id = Guid.NewGuid().ToString(),
                Content = replyDTO.Content,
                UserId = userId,
                CourseId = parent.CourseId,
                ReplyId = parent.Id,
                CreatedAt = DateTime.UtcNow,
                Type = CommentType.Reply
            };

            await _courseRepository.AddCommentAsync(reply);
            await _courseRepository.SaveChangesAsync();

            return new ApiResponse("Created", _localizer["ReplyAdded"].Value, null, true);
        }

        public async Task<ApiResponse> MarkItemCompletedAsync(MarkItemCompletedDTO dto, string studentId)
        {
            var progress = await _courseRepository.GetProgressAsync(studentId, dto.LectureId, dto.ItemId, dto.ItemType);
            if (progress == null)
            {
                progress = new StudentLectureProgress { Id = Guid.NewGuid().ToString(), StudentId = studentId, LectureId = dto.LectureId, CourseId = dto.CourseId, ItemId = dto.ItemId, ItemType = dto.ItemType, IsCompleted = true };
                await _courseRepository.AddProgressAsync(progress);
            }
            else
            {
                progress.IsCompleted = true;
                await _courseRepository.UpdateProgressAsync(progress);
            }
            await _courseRepository.SaveChangesAsync();
            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> UnmarkItemCompletedAsync(MarkItemCompletedDTO dto, string studentId)
        {
            var progress = await _courseRepository.GetProgressAsync(studentId, dto.LectureId, dto.ItemId, dto.ItemType);
            if (progress != null)
            {
                progress.IsCompleted = false;
                await _courseRepository.UpdateProgressAsync(progress);
                await _courseRepository.SaveChangesAsync();
            }
            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> GetContinueLearningCoursesAsync(string studentId)
        {
            var enrollments = await _courseRepository.GetEnrollmentsQueryable()
                .AsNoTracking()
                .Include(e => e.Course).ThenInclude(c => c.Instructor)
                .Where(e => e.StudentId == studentId && e.Status == true)
                .OrderByDescending(e => e.LastVisit)
                .Take(5)
                .ToListAsync();

            var result = enrollments.Select(e => new MyCourseDTO { Id = e.Course.Id, Name = e.Course.Name, ImageUrl = e.Course.ImageUrl, InstructorName = e.Course.Instructor.FullName }).ToList();
            return new ApiResponse("Success", _localizer["Success"].Value, result, true);
        }

        #endregion

        #region Instructor Features
        public async Task<ApiResponse> GetCoursesByInstructorAsync(string instructorId, int pageNumber, int pageSize)
        {
            var query = _courseRepository.GetQueryable()
                .AsNoTracking()
                .Include(c => c.Enrollments).ThenInclude(e => e.Comments)
                .Where(c => c.InstructorId == instructorId);

            var totalCount = await query.CountAsync();
            var courses = await query.OrderByDescending(c => c.CreateTime).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            var result = courses.Select(c => new InstructorCourseListDTO
            {
                Id = c.Id,
                Name = c.Name,
                ImageUrl = c.ImageUrl,
                Price = c.Price,
                TotalStudents = c.Enrollments.Count,
                Rating = c.Enrollments.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).Any() ? c.Enrollments.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).Average(cm => cm.Rate) : 0,
                Status = c.Status.ToString()
            }).ToList();

            return new ApiResponse("Success", _localizer["Success"].Value, new PagedResult<InstructorCourseListDTO> { Items = result, Page = pageNumber, PageSize = pageSize, TotalCount = totalCount }, true);
        }

        public async Task<ApiResponse> GetInstructorDashboardAsync(string instructorId)
        {
            var courses = await _courseRepository.GetQueryable()
                .AsNoTracking()
                .Where(c => c.InstructorId == instructorId)
                .Include(c => c.Enrollments).ThenInclude(e => e.Comments)
                .ToListAsync();

            var totalStudents = courses.Sum(c => c.Enrollments.Count);
            var totalRevenue = (long)courses.Sum(c => c.Price * c.Enrollments.Count);
            var ratings = courses.SelectMany(c => c.Enrollments.SelectMany(e => e.Comments)).Where(cm => cm.Type == CommentType.Review).ToList();
            var avgRating = ratings.Any() ? ratings.Average(cm => cm.Rate) : 0;

            var dashboard = new InstructorDashboardDTO
            {
                TotalStudents = totalStudents,
                TotalRevenue = totalRevenue,
                AverageRating = Math.Round(avgRating, 1),
                TotalCourses = courses.Count
            };

            return new ApiResponse("Success", _localizer["Success"].Value, dashboard, true);
        }

        public async Task<ApiResponse> GetInstructorActivitiesAsync(string instructorId, int page, int pageSize)
        {
            return new ApiResponse("Success", _localizer["Success"].Value, new PagedResult<RecentActivityDTO> { Items = new List<RecentActivityDTO>(), Page = page, PageSize = pageSize, TotalCount = 0 }, true);
        }

        public async Task<ApiResponse> GetInstructorUnreadThreadsAsync(string instructorId)
        {
            return new ApiResponse("Success", _localizer["Success"].Value, new List<UnreadThreadCourseDTO>(), true);
        }
        #endregion

        #region QA Features
        public async Task<ApiResponse> GetCourseQAThreadsAsync(string courseId, string userId, int pageNumber, int pageSize, string filter = "all")
        {
            var query = _courseRepository.GetThreadsQueryable().AsNoTracking().Where(t => t.CourseId == courseId);
            var totalCount = await query.CountAsync();
            var threads = await query.Include(t => t.Creator).OrderByDescending(t => t.LastActivityAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            var result = threads.Select(t => new QAThreadDTO { Id = t.Id, Title = t.Title, CreatorName = t.Creator.FullName, CreatedAt = t.CreatedAt, LastActivityAt = t.LastActivityAt }).ToList();
            return new ApiResponse("Success", _localizer["Success"].Value, new PagedResult<QAThreadDTO> { Items = result, Page = pageNumber, PageSize = pageSize, TotalCount = totalCount }, true);
        }

        public async Task<ApiResponse> GetThreadMessagesAsync(string threadId, string userId, int pageNumber, int pageSize)
        {
            var thread = await _courseRepository.GetThreadByIdAsync(threadId);
            if (thread == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            var query = _courseRepository.GetMessagesQueryable().AsNoTracking().Where(m => m.ThreadId == threadId);
            var totalCount = await query.CountAsync();
            var messages = await query.Include(m => m.User).OrderBy(m => m.CreatedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            var result = messages.Select(m => new QAMessageDTO { Id = m.Id, Content = m.Content, UserName = m.User.FullName, CreatedAt = m.CreatedAt }).ToList();
            return new ApiResponse("Success", _localizer["Success"].Value, new { thread = new QAThreadDetailDTO { Id = thread.Id, Title = thread.Title }, messages = new PagedResult<QAMessageDTO> { Items = result, Page = pageNumber, PageSize = pageSize, TotalCount = totalCount } }, true);
        }

        public async Task<ApiResponse> CreateQAThreadAsync(CreateThreadDTO createThreadDTO, string userId)
        {
            var thread = new QAThread { Id = Guid.NewGuid().ToString(), CourseId = createThreadDTO.CourseId, CreatorId = userId, Title = createThreadDTO.Title, CreatedAt = DateTime.UtcNow, LastActivityAt = DateTime.UtcNow };
            await _courseRepository.AddThreadAsync(thread);
            await _courseRepository.SaveChangesAsync();
            return new ApiResponse("Created", _localizer["Success"].Value, thread.Id, true);
        }

        public async Task<ApiResponse> AddMessageToThreadAsync(AddMessageDTO addMessageDTO, string userId)
        {
            var thread = await _courseRepository.GetThreadByIdAsync(addMessageDTO.ThreadId);
            if (thread == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            var message = new QAMessage { Id = Guid.NewGuid().ToString(), ThreadId = thread.Id, UserId = userId, Content = addMessageDTO.Content, CreatedAt = DateTime.UtcNow };
            thread.LastActivityAt = DateTime.UtcNow;
            await _courseRepository.AddMessageAsync(message);
            await _courseRepository.SaveChangesAsync();
            return new ApiResponse("Created", _localizer["Success"].Value, message.Id, true);
        }

        public async Task<ApiResponse> UpdateQAThreadAsync(string threadId, UpdateThreadDTO updateThreadDTO, string userId)
        {
            var thread = await _courseRepository.GetThreadByIdAsync(threadId);
            if (thread == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            if (thread.CreatorId != userId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            thread.Title = updateThreadDTO.Title;
            await _courseRepository.UpdateThreadAsync(thread);
            await _courseRepository.SaveChangesAsync();
            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> UpdateQAMessageAsync(string messageId, UpdateMessageDTO updateMessageDTO, string userId)
        {
            var message = await _courseRepository.GetMessageByIdAsync(messageId);
            if (message == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            if (message.UserId != userId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            message.Content = updateMessageDTO.Content;
            message.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.UpdateMessageAsync(message);
            await _courseRepository.SaveChangesAsync();
            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> DeleteQAThreadAsync(string threadId, string userId)
        {
            var thread = await _courseRepository.GetThreadByIdAsync(threadId);
            if (thread == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            if (thread.CreatorId != userId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            await _courseRepository.DeleteThreadAsync(thread);
            await _courseRepository.SaveChangesAsync();
            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> DeleteQAMessageAsync(string messageId, string userId)
        {
            var message = await _courseRepository.GetMessageByIdAsync(messageId);
            if (message == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            if (message.UserId != userId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            await _courseRepository.DeleteMessageAsync(message);
            await _courseRepository.SaveChangesAsync();
            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }
        #endregion

        #region Wishlist Features
        public async Task<ApiResponse> AddToWishlistAsync(string courseId, string studentId)
        {
            var exists = await _courseRepository.GetWishlistItemAsync(studentId, courseId);
            if (exists != null) return new ApiResponse("Conflict", _localizer["AlreadyInWishlist"].Value, null, false);
            var wishlist = new Wishlist { Id = Guid.NewGuid().ToString(), StudentId = studentId, CourseId = courseId, AddedAt = DateTime.UtcNow };
            await _courseRepository.AddToWishlistAsync(wishlist);
            await _courseRepository.SaveChangesAsync();
            return new ApiResponse("Created", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> RemoveFromWishlistAsync(string courseId, string studentId)
        {
            var wishlist = await _courseRepository.GetWishlistItemAsync(studentId, courseId);
            if (wishlist == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            await _courseRepository.RemoveFromWishlistAsync(wishlist);
            await _courseRepository.SaveChangesAsync();
            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> GetStudentWishlistAsync(string studentId, int pageNumber, int pageSize)
        {
            var query = _courseRepository.GetWishlistQueryable().AsNoTracking().Where(w => w.StudentId == studentId);
            var totalCount = await query.CountAsync();
            var items = await query.Include(w => w.Course).ThenInclude(c => c.Instructor).OrderByDescending(w => w.AddedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            var result = items.Select(w => new CourseCardDTO { Id = w.Course.Id, Name = w.Course.Name, ImageUrl = w.Course.ImageUrl, InstructorName = w.Course.Instructor.FullName, Price = w.Course.Price }).ToList();
            return new ApiResponse("Success", _localizer["Success"].Value, new PagedResult<CourseCardDTO> { Items = result, Page = pageNumber, PageSize = pageSize, TotalCount = totalCount }, true);
        }
        #endregion

        #region Admin Requests
        public async Task<ApiResponse> CreateCourseRequestAsync(string courseId, string instructorId)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null || course.InstructorId != instructorId) return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            var request = new CourseRequest { Id = Guid.NewGuid().ToString(), CourseId = courseId, Status = RequestStatus.Pending, CreatedAt = DateTime.UtcNow };
            await _courseRepository.AddRequestAsync(request);
            await _courseRepository.SaveChangesAsync();
            return new ApiResponse("Created", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> GetPendingCourseRequestsAsync(int pageNumber, int pageSize)
        {
            var query = _courseRepository.GetRequestsQueryable().Include(cr => cr.Course).ThenInclude(c => c.Instructor).Where(cr => cr.Status == RequestStatus.Pending);
            var totalCount = await query.CountAsync();
            var requests = await query.OrderBy(cr => cr.CreatedAt).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            var result = requests.Select(cr => new CourseRequestDTO { Id = cr.Id, CourseId = cr.CourseId, CourseName = cr.Course.Name, InstructorName = cr.Course.Instructor.FullName, Status = cr.Status.ToString(), CreatedAt = cr.CreatedAt }).ToList();
            return new ApiResponse("Success", _localizer["Success"].Value, new PagedResult<CourseRequestDTO> { Items = result, Page = pageNumber, PageSize = pageSize, TotalCount = totalCount }, true);
        }

        public async Task<ApiResponse> ApproveCourseRequestAsync(string requestId, ResponseRequestDTO responseRequestDTO)
        {
            var request = await _courseRepository.GetRequestsQueryable().Include(r => r.Course).FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            request.Status = RequestStatus.Approved;
            request.Course.Status = CourseStatus.Public;
            await _courseRepository.SaveChangesAsync();
            _backgroundJobClient.Enqueue(() => _luceneSearchService.IndexCourseAsync(request.Course));
            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> RejectCourseRequestAsync(string requestId, ResponseRequestDTO responseRequestDTO)
        {
            var request = await _courseRepository.GetRequestByIdAsync(requestId);
            if (request == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            request.Status = RequestStatus.Rejected;
            await _courseRepository.SaveChangesAsync();
            return new ApiResponse("Success", _localizer["Success"].Value, null, true);
        }

        public async Task<ApiResponse> GetAllCoursesForAdminAsync(int pageNumber, int pageSize)
        {
            var query = _courseRepository.GetQueryable().AsNoTracking().Include(c => c.Instructor).Include(c => c.Enrollments);
            var totalCount = await query.CountAsync();
            var courses = await query.OrderByDescending(c => c.CreateTime).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            var result = courses.Select(c => new CourseListDTO { Id = c.Id, ImageUrl = c.ImageUrl, Name = c.Name, InstructorName = c.Instructor.FullName, Price = c.Price, TotalStudents = c.Enrollments.Count, Status = c.Status.ToString() }).ToList();
            return new ApiResponse("Success", _localizer["Success"].Value, new PagedResult<CourseListDTO> { Items = result, Page = pageNumber, PageSize = pageSize, TotalCount = totalCount }, true);
        }
        #endregion

        #region Helper Methods
        private async Task RemoveRecommendedCache() { await _cache.RemoveAsync("course:recommended:guest"); }
        #endregion
    }
}
