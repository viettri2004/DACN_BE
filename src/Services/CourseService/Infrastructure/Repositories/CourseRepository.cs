using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using CourseService.Domain.Enums;
using CourseService.Domain.Entities;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using Shared.Infrastructure.cloudinaryService;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using AccountService.Application.Interfaces;
using AccountService.Domain.Enums;
using CartService.Application.DTOs;
using System.Text;
using Hangfire;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using src.Shared.Infrastructure;

namespace CourseService.Infrastructure.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryService _cloudinaryService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILuceneSearchService _luceneSearchService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IAiService _aiService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IDistributedCache _cache;

        public CourseRepository(AppDbContext context,
                               CloudinaryService cloudinaryService,
                               IStringLocalizer<SharedResources> localizer,
                               ILuceneSearchService luceneSearchService,
                               INotificationRepository notificationRepository,
                               IAiService aiService,
                               IBackgroundJobClient backgroundJobClient,
                               IDistributedCache cache)
        {
            _context = context;
            _luceneSearchService = luceneSearchService;
            _cloudinaryService = cloudinaryService;
            _localizer = localizer;
            _notificationRepository = notificationRepository;
            _aiService = aiService;
            _backgroundJobClient = backgroundJobClient;
            _cache = cache;
        }

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
                    foreach (var tagId in createCourseDTO.TagIds)
                    {
                        var tag = await _context.Tags.FindAsync(tagId);
                        if (tag != null)
                        {
                            newCourse.CourseTags.Add(new CourseTag
                            {
                                CourseId = newCourse.Id,
                                TagId = tagId
                            });
                        }
                    }
                }

                _context.Courses.Add(newCourse);
                await _context.SaveChangesAsync();
                await RemoveRecommendedCache();

                try
                {
                    var courseForIndex = await _context.Courses
                        .AsNoTracking()
                        .Include(c => c.Instructor)
                        .Include(c => c.CourseTags)
                        .Include(c => c.Enrollments)
                            .ThenInclude(e => e.Comments)
                        .FirstOrDefaultAsync(c => c.Id == newCourse.Id);

                    if (courseForIndex != null)
                    {
                        await _luceneSearchService.IndexCourseAsync(courseForIndex);
                    }
                }
                catch (Exception indexEx)
                {
                    Console.WriteLine($"Indexing course failed: {indexEx.Message}");
                }

                return new ApiResponse(
                    "Created",
                    _localizer["CreateCourseSuccess"].Value,
                    new InstructorCourseListDTO
                    {
                        Id = newCourse.Id,
                        Name = newCourse.Name,
                        ImageUrl = newCourse.ImageUrl,
                        Price = newCourse.Price,
                        TotalStudents = 0,
                        Rating = 0,
                        Status = newCourse.Status.ToString()
                    },
                    true
                );
            }
            catch (Exception)
            {
                return new ApiResponse("Error", _localizer["CourseCreationFailed"].Value, null, false);
            }
        }

        public async Task<ApiResponse> UpdateCourseAsync(string courseId, UpdateCourseDTO updateCourseDTO, string instructorId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.CourseTags)
                    .FirstOrDefaultAsync(c => c.Id == courseId);

                if (course == null)
                {
                    return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);
                }

                if (course.InstructorId != instructorId)
                {
                    return new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false);
                }

                course.Name = updateCourseDTO.name;
                course.Price = updateCourseDTO.price;
                course.Description = updateCourseDTO.description ?? string.Empty;
                course.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(updateCourseDTO.imageUrl) && !string.IsNullOrEmpty(updateCourseDTO.imagePublicId))
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(course.ImagePublicId))
                    {
                        await _cloudinaryService.DeleteImageAsync(course.ImagePublicId);
                    }

                    course.ImageUrl = updateCourseDTO.imageUrl;
                    course.ImagePublicId = updateCourseDTO.imagePublicId;
                }

                // Update tags
                if (updateCourseDTO.TagIds != null)
                {
                    // Remove old tags
                    _context.CourseTags.RemoveRange(course.CourseTags);

                    // Add new tags
                    foreach (var tagId in updateCourseDTO.TagIds)
                    {
                        var tag = await _context.Tags.FindAsync(tagId);
                        if (tag != null)
                        {
                            course.CourseTags.Add(new CourseTag
                            {
                                CourseId = course.Id,
                                TagId = tagId
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await RemoveRecommendedCache();
                await RemoveCourseDetailCache(courseId);

                // Re-index
                try
                {
                    var courseForIndex = await _context.Courses
                        .AsNoTracking()
                        .Include(c => c.Instructor)
                        .Include(c => c.CourseTags)
                        .Include(c => c.Enrollments)
                            .ThenInclude(e => e.Comments)
                        .FirstOrDefaultAsync(c => c.Id == course.Id);

                    if (courseForIndex != null)
                    {
                        await _luceneSearchService.IndexCourseAsync(courseForIndex);
                    }
                }
                catch (Exception indexEx)
                {
                    Console.WriteLine($"Indexing course failed: {indexEx.Message}");
                }

                return new ApiResponse("Success", _localizer["CourseUpdated"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> GetCourseDetailAsync(string courseId, string studentId)
        {
            string cacheKey = $"course:detail:{courseId}:{studentId ?? "guest"}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<ApiResponse>(cachedData, JsonSettings.CamelCase);
            }

            var course = await _context.Courses
                .AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.Instructor)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Comments)
                .Include(c => c.Lectures.OrderBy(l => l.DisplayOrder))
                    .ThenInclude(l => l.LectureVideos.OrderBy(lv => lv.DisplayOrder))
                .Include(c => c.Lectures)
                    .ThenInclude(l => l.Quizzes)
                .Include(c => c.Lectures)
                    .ThenInclude(l => l.Documents)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            bool isEnrolled = !string.IsNullOrEmpty(studentId) &&
                              course.Enrollments.Any(e => e.StudentId == studentId && e.Status == true);

            var totalSeconds = course.Lectures
                .SelectMany(l => l.LectureVideos)
                .Sum(lv => lv.Duration);
            var totalHours = totalSeconds / 3600.0;

            var allVideos = course.Lectures
                .OrderBy(l => l.DisplayOrder)
                .SelectMany(l => l.LectureVideos.OrderBy(lv => lv.DisplayOrder))
                .ToList();



            var totalInstructorCourses = await _context.Courses
                .CountAsync(c => c.InstructorId == course.InstructorId);

            var courseDetailDto = new CourseDetailDTO
            {
                Name = course.Name,
                Description = course.Description,
                Price = course.Price,
                ImageUrl = course.ImageUrl,
                InstructorName = course.Instructor.FullName,
                InstructorJobPosition = course.Instructor.JobPosition ?? _localizer["DefaultInstructorJobPosition"].Value,
                InstructorTotalCourses = totalInstructorCourses,
                Rating = course.Enrollments.SelectMany(e => e.Comments).Any(cm => cm.Type == CommentType.Review)
                        ? course.Enrollments.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).Average(cm => cm.Rate)
                        : 0,
                TotalReviews = course.Enrollments.SelectMany(e => e.Comments).Count(cm => cm.Type == CommentType.Review),
                TotalStudents = course.Enrollments.Count,
                TotalHours = totalSeconds > 0 ? Math.Max(0.1, Math.Round(totalHours, 2)) : 0,
                IsEnrolled = isEnrolled,
                UpdatedAt = course.UpdatedAt == default ? course.CreateTime : course.UpdatedAt,
                LastUpdate = course.UpdatedAt == default ? course.CreateTime : course.UpdatedAt,
                Lectures = course.Lectures.OrderBy(l => l.DisplayOrder).Select(l => new LecturePreviewDTO
                {
                    // Id = l.Id,
                    Name = l.Name,
                    Description = l.Description,
                    DisplayOrder = l.DisplayOrder,
                    Videos = l.LectureVideos.OrderBy(lv => lv.DisplayOrder).Select(lv => new VideoPreviewDTO
                    {
                        // Id = lv.Id,
                        Name = lv.Name,
                        Duration = lv.Duration,
                        DisplayOrder = lv.DisplayOrder,
                        IsTrial = allVideos.IndexOf(lv) < 2,
                        VideoUrl = (isEnrolled || allVideos.IndexOf(lv) < 2) ? lv.VideoUrl : null
                    }).ToList(),
                    Quizzes = l.Quizzes.Select(q => new QuizPreviewDTO
                    {
                        // Id = q.Id,
                        Name = q.Name
                    }).ToList(),
                    Documents = l.Documents.Select(d => new DocumentPreviewDTO
                    {
                        // Id = d.Id,
                        Name = d.Name
                    }).ToList()
                }).ToList()
            };

            var response = new ApiResponse("Success", _localizer["Success"].Value, courseDetailDto, true);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            };
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response, JsonSettings.CamelCase), cacheOptions);

            return response;
        }

        public async Task<ApiResponse> GetCourseCommentsAsync(string courseId, string? userId, CommentType type, int pageNumber, int pageSize, int? rating = null)
        {
            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null)
                return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);

            bool isInstructor = userId != null && course.InstructorId == userId;

            var commentQuery = _context.Comments
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .Where(c => c.CourseId == courseId && c.ReplyId == null && c.Type == type);

            if (rating.HasValue && type == CommentType.Review)
            {
                commentQuery = commentQuery.Where(c => c.Rate == rating.Value);
            }

            var totalCount = await commentQuery.CountAsync();

            var comments = await commentQuery
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CommentDTO
                {
                    CommentId = c.Id,
                    UserName = c.User.FullName,
                    AvatarUrl = c.User.AvatarUrl,
                    Rate = c.Rate,
                    Content = c.Content,
                    Type = c.Type,
                    IsMyComment = userId != null && c.UserId == userId,
                    CanDelete = userId != null && (course.InstructorId == userId || c.UserId == userId),
                    Timestamp = c.CreatedAt,
                    Replies = c.Replies
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReplyDTO
                    {
                        CommentId = r.Id,
                        Content = r.Content,
                        Timestamp = r.CreatedAt,
                        Type = r.Type,
                        IsMyComment = userId != null && r.UserId == userId,
                        CanDelete = userId != null && (course.InstructorId == userId || r.UserId == userId)
                    }).ToList()
                })
                .ToListAsync();

            var response = new CourseCommentsResponseDTO
            {
                IsInstructor = isInstructor,
                MyComment = userId != null ? await _context.Comments
                    .AsNoTracking()
                    .Include(c => c.User)
                    .Where(c => c.CourseId == courseId && c.UserId == userId && c.Type == CommentType.Review && c.ReplyId == null)
                    .Select(c => new CommentDTO
                    {
                        CommentId = c.Id,
                        UserName = c.User.FullName,
                        AvatarUrl = c.User.AvatarUrl,
                        Rate = c.Rate,
                        Content = c.Content,
                        Type = c.Type,
                        IsMyComment = true,
                        CanDelete = true,
                        Timestamp = c.CreatedAt,
                        Replies = c.Replies
                        .OrderByDescending(r => r.CreatedAt)
                        .Select(r => new ReplyDTO
                        {
                            CommentId = r.Id,
                            Content = r.Content,
                            Timestamp = r.CreatedAt,
                            Type = r.Type,
                            IsMyComment = userId != null && r.UserId == userId,
                            CanDelete = true
                        }).ToList()
                    }).FirstOrDefaultAsync() : null,
                AllComments = comments.Where(c => !(userId != null && c.IsMyComment && c.Type == CommentType.Review)).ToList()
            };

            var pagedResult = new PagedResult<CourseCommentsResponseDTO>
            {
                Items = new List<CourseCommentsResponseDTO> { response },
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return new ApiResponse("Success", _localizer["Success"].Value, pagedResult, true);
        }

        public async Task<ApiResponse> GetRecommendedCoursesAsync(string? userId)
        {
            string cacheKey = string.IsNullOrEmpty(userId) ? "course:recommended:guest" : $"course:recommended:user:{userId}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<ApiResponse>(cachedData, JsonSettings.CamelCase);
            }

            var enrolledCourseIds = new List<string>();
            var enrolledTags = new List<string>();

            if (!string.IsNullOrEmpty(userId))
            {
                var userEnrollments = await _context.Enrollments
                    .AsNoTracking()
                    .Include(e => e.Course)
                        .ThenInclude(c => c.CourseTags)
                    .Where(e => e.StudentId == userId && e.Status == true)
                    .ToListAsync();

                enrolledCourseIds = userEnrollments.Select(e => e.CourseId).ToList();
                enrolledTags = userEnrollments
                    .SelectMany(e => e.Course.CourseTags)
                    .Select(ct => ct.TagId)
                    .Distinct()
                    .ToList();
            }

            IQueryable<Course> query = _context.Courses
                .AsNoTracking()
                .Include(c => c.Instructor)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Comments)
                .Where(c => c.Status == CourseStatus.Public && !enrolledCourseIds.Contains(c.Id));

            if (enrolledTags.Any())
            {
                // Logic: Recommend courses that have AT LEAST ONE of the tags from user's current courses
                query = query.Where(c => c.CourseTags.Any(ct => enrolledTags.Contains(ct.TagId)));
            }
            
            // If the user has tags, we still might want to order by popularity within those tags
            // If the user has no tags (not logged in or no courses), this is purely popularity based
            var courses = await query
                .OrderByDescending(c => c.Enrollments.Count)
                .ThenByDescending(c => c.Enrollments
                    .SelectMany(e => e.Comments)
                    .Where(cm => cm.Type == CommentType.Review)
                    .Average(cm => (double?)cm.Rate) ?? 0)
                .Take(10) // Take a bit more to ensure variety
                .Select(c => new CourseListDTO
                {
                    Id = c.Id,
                    ImageUrl = c.ImageUrl,
                    Name = c.Name,
                    InstructorName = c.Instructor.FullName,
                    Rating = c.Enrollments
                                .SelectMany(e => e.Comments)
                                .Where(cm => cm.Type == CommentType.Review)
                                .Average(cm => (double?)cm.Rate) ?? 0,
                    Price = c.Price,
                    TotalStudents = c.Enrollments.Count
                })
                .ToListAsync();

            if (courses.Count == 0 && enrolledTags.Any())
            {
                // Fallback to popularity if no tag-based matches are found (e.g. all courses with those tags already enrolled)
                return await GetRecommendedCoursesAsync(null); 
            }

            if (courses.Count == 0)
                return new ApiResponse("Success", _localizer["NoData"].Value, null, true);

            var response = new ApiResponse("Success", _localizer["Success"].Value, courses.Take(3), true);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response, JsonSettings.CamelCase), cacheOptions);

            return response;
        }


        public async Task<ApiResponse> GetCoursesByStudentIdAsync(string studentId)
        {
            var courseDTOs = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId && e.Status == true)
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => new CourseListDTO
                {
                    Id = e.Course.Id,
                    ImageUrl = e.Course.ImageUrl,
                    Name = e.Course.Name,
                    InstructorName = e.Course.Instructor.FullName,
                    Rating = _context.Comments
                        .Where(c => c.Enrollment.CourseId == e.CourseId && c.Type == CommentType.Review)
                        .Any()
                            ? _context.Comments
                                .Where(c => c.Enrollment.CourseId == e.CourseId && c.Type == CommentType.Review)
                                .Average(c => c.Rate)
                            : 0,
                    Price = e.Order.OrderItems
                        .Where(oi => oi.CourseId == e.CourseId)
                        .Sum(oi => oi.Price),
                    Progress = (e.Course.Lectures.SelectMany(l => l.LectureVideos).Count() +
                                e.Course.Lectures.SelectMany(l => l.Documents).Count() +
                                e.Course.Lectures.SelectMany(l => l.Quizzes).Count()) == 0 ? 0 :
                               (_context.StudentLectureProgresses
                                    .Count(p => p.CourseId == e.CourseId && p.StudentId == studentId && p.IsCompleted) * 100) /
                               (e.Course.Lectures.SelectMany(l => l.LectureVideos).Count() +
                                e.Course.Lectures.SelectMany(l => l.Documents).Count() +
                                e.Course.Lectures.SelectMany(l => l.Quizzes).Count()),
                    TotalLessons = e.Course.Lectures.SelectMany(l => l.LectureVideos).Count() +
                                   e.Course.Lectures.SelectMany(l => l.Documents).Count() +
                                   e.Course.Lectures.SelectMany(l => l.Quizzes).Count(),
                    CompletedLessons = _context.StudentLectureProgresses
                                    .Count(p => p.CourseId == e.CourseId && p.StudentId == studentId && p.IsCompleted),
                    EnrolledAt = e.EnrolledAt,
                    LastVisit = e.LastVisit,
                    TotalHours = e.Course.Lectures.SelectMany(l => l.LectureVideos).Any()
                        ? Math.Max(0.1, Math.Round(e.Course.Lectures.SelectMany(l => l.LectureVideos).Sum(v => v.Duration) / 3600.0, 2))
                        : 0
                })
                .ToListAsync();

            if (!courseDTOs.Any())
                return new ApiResponse("Success", _localizer["NoData"].Value, new StudentCourseResponseDTO(), true);

            // Calculate overall stats
            var totalStudyTimeSeconds = await _context.StudentLectureProgresses
                .Where(p => p.StudentId == studentId && p.IsCompleted)
                .Join(_context.LectureVideos, p => p.ItemId, lv => lv.Id, (p, lv) => lv.Duration)
                .SumAsync();

            var summary = new StudentCourseResponseDTO
            {
                TotalCourses = courseDTOs.Count,
                CompletedCourses = courseDTOs.Count(c => c.Progress == 100),
                TotalStudyTime = Math.Round(totalStudyTimeSeconds / 3600.0, 2),
                AverageProgress = Math.Round(courseDTOs.Average(c => c.Progress), 1),
                Courses = courseDTOs
            };

            return new ApiResponse("Success", _localizer["Success"].Value, summary, true);
        }

        public async Task<ApiResponse> GetContinueLearningCoursesAsync(string studentId)
        {
            var courseDTOs = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId && e.Status == true)
                .OrderByDescending(e => e.LastVisit)
                .Take(3)
                .Select(e => new CourseListDTO
                {
                    Id = e.Course.Id,
                    ImageUrl = e.Course.ImageUrl,
                    Name = e.Course.Name,
                    InstructorName = e.Course.Instructor.FullName,
                    Rating = _context.Comments
                        .Where(c => c.Enrollment.CourseId == e.CourseId && c.Type == CommentType.Review)
                        .Any()
                            ? _context.Comments
                                .Where(c => c.Enrollment.CourseId == e.CourseId && c.Type == CommentType.Review)
                                .Average(c => c.Rate)
                            : 0,
                    Price = e.Order.OrderItems
                        .Where(oi => oi.CourseId == e.CourseId)
                        .Sum(oi => oi.Price),
                    Progress = (e.Course.Lectures.SelectMany(l => l.LectureVideos).Count() +
                                e.Course.Lectures.SelectMany(l => l.Documents).Count() +
                                e.Course.Lectures.SelectMany(l => l.Quizzes).Count()) == 0 ? 0 :
                               (_context.StudentLectureProgresses
                                    .Count(p => p.CourseId == e.CourseId && p.StudentId == studentId && p.IsCompleted) * 100) /
                               (e.Course.Lectures.SelectMany(l => l.LectureVideos).Count() +
                                e.Course.Lectures.SelectMany(l => l.Documents).Count() +
                                e.Course.Lectures.SelectMany(l => l.Quizzes).Count()),
                    TotalLessons = e.Course.Lectures.SelectMany(l => l.LectureVideos).Count() +
                                   e.Course.Lectures.SelectMany(l => l.Documents).Count() +
                                   e.Course.Lectures.SelectMany(l => l.Quizzes).Count(),
                    CompletedLessons = _context.StudentLectureProgresses
                                    .Count(p => p.CourseId == e.CourseId && p.StudentId == studentId && p.IsCompleted),
                    EnrolledAt = e.EnrolledAt,
                    LastVisit = e.LastVisit,
                    TotalHours = e.Course.Lectures.SelectMany(l => l.LectureVideos).Any()
                        ? Math.Max(0.1, Math.Round(e.Course.Lectures.SelectMany(l => l.LectureVideos).Sum(v => v.Duration) / 3600.0, 2))
                        : 0
                })
                .ToListAsync();

            return new ApiResponse("Success", _localizer["Success"].Value, courseDTOs, true);
        }

        public async Task<ApiResponse> GetCoursesByInstructorAsync(string instructorId)
        {
            var courseDTOs = await _context.Courses
                .AsNoTracking()
                .Where(c => c.InstructorId == instructorId)
                .OrderByDescending(c => c.CreateTime)
                .Select(c => new InstructorCourseListDTO
                {
                    Id = c.Id,
                    ImageUrl = c.ImageUrl,
                    Name = c.Name,
                    Rating = c.Enrollments
                        .SelectMany(e => e.Comments)
                        .Where(cm => cm.Type == CommentType.Review)
                        .Any()
                            ? c.Enrollments
                                .SelectMany(e => e.Comments)
                                .Where(cm => cm.Type == CommentType.Review)
                                .Average(cm => cm.Rate)
                            : 0,
                    Price = c.Price,
                    TotalStudents = c.Enrollments.Count,
                    Status = c.CourseRequests.Any(r => r.Status == RequestStatus.Pending)
                             ? "Pending"
                             : c.Status.ToString()
                })
                .ToListAsync();

            if (courseDTOs.Count == 0)
                return new ApiResponse("Success", _localizer["NoData"].Value, null, true);

            return new ApiResponse("Success", _localizer["Success"].Value, courseDTOs, true);
        }

        public async Task<ApiResponse> GetCourseContentAsync(string courseId, string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new ApiResponse("NotFound", _localizer["UserNotFound"].Value, null, false);

            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Instructor)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Comments)
                .Include(c => c.Lectures.OrderBy(l => l.DisplayOrder))
                    .ThenInclude(l => l.LectureVideos.OrderBy(lv => lv.DisplayOrder))
                .Include(c => c.Lectures)
                    .ThenInclude(l => l.Quizzes)
                .Include(c => c.Lectures)
                    .ThenInclude(l => l.Documents)
                .Include(c => c.CourseTags)
                    .ThenInclude(ct => ct.Tag)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
            {
                return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            }

            // Authorization check
            bool isAuthorized = false;

            if (user is Admin)
            {
                isAuthorized = true;
            }
            else if (user is Instructor && course.InstructorId == userId)
            {
                isAuthorized = true;
            }
            else
            {
                // Check enrollment for students
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId && e.Status == true);

                if (enrollment != null)
                {
                    isAuthorized = true;
                    enrollment.LastVisit = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            if (!isAuthorized)
            {
                return new ApiResponse("Forbidden", _localizer["ForbiddenCourseContent"].Value, null, false);
            }

            var completedItems = await _context.StudentLectureProgresses
                .Where(p => p.CourseId == courseId && p.StudentId == userId && p.IsCompleted)
                .Select(p => new { p.ItemId, p.ItemType, p.LectureId })
                .ToListAsync();

            var completedItemIds = new HashSet<string>(completedItems.Select(x => x.ItemId));
            var fullyCompletedLectureIds = new HashSet<string>();

            foreach (var lecture in course.Lectures)
            {
                int totalItemsInLecture = lecture.LectureVideos.Count + lecture.Documents.Count + lecture.Quizzes.Count;
                int completedItemsInLecture = completedItems.Count(x => x.LectureId == lecture.Id);

                if (totalItemsInLecture > 0 && completedItemsInLecture == totalItemsInLecture)
                {
                    fullyCompletedLectureIds.Add(lecture.Id);
                }
            }

            int totalItemsInCourse = course.Lectures.Sum(l => l.LectureVideos.Count + l.Documents.Count + l.Quizzes.Count);
            int progressPercentage = totalItemsInCourse > 0 ? (completedItems.Count * 100) / totalItemsInCourse : 0;

            var totalSeconds = course.Lectures
                .SelectMany(l => l.LectureVideos)
                .Sum(lv => lv.Duration);
            var totalHours = totalSeconds / 3600.0;

            var completedVideoIds = completedItems
                .Where(x => x.ItemType == "Video")
                .Select(x => x.ItemId)
                .ToList();

            var completedVideoDurations = await _context.LectureVideos
                .Where(v => completedVideoIds.Contains(v.Id))
                .Select(v => v.Duration)
                .ToListAsync();

            double totalStudyTimeHours = Math.Round(completedVideoDurations.Sum() / 3600.0, 2);

            var courseContent = new CourseContentDTO
            {
                Id = course.Id,
                Name = course.Name,
                Progress = progressPercentage,
                TotalSections = course.Lectures.Count,
                Tags = course.CourseTags?.Select(t => t.Tag.Name).ToList() ?? new List<string>(),
                TotalHours = totalSeconds > 0 ? Math.Max(0.1, Math.Round(totalHours, 2)) : 0,
                TotalLessons = totalItemsInCourse,
                CompletedLessons = completedItems.Count,
                TotalStudyTime = totalStudyTimeHours,
                Lectures = course.Lectures
                .OrderBy(l => l.DisplayOrder)
                .Select(l => new LectureContentDTO
                {
                    Id = l.Id,
                    Name = l.Name,
                    Description = l.Description,
                    DisplayOrder = l.DisplayOrder,
                    IsCompleted = fullyCompletedLectureIds.Contains(l.Id),
                    Videos = l.LectureVideos
                    .OrderBy(v => v.DisplayOrder)
                    .Select(v => new VideoContentDTO
                    {
                        Id = v.Id,
                        DisplayOrder = v.DisplayOrder,
                        Name = v.Name,
                        Duration = v.Duration,
                        IsCompleted = completedItemIds.Contains(v.Id)
                    }).ToList(),
                    Documents = l.Documents.Select(d => new DocumentContentDTO
                    {
                        Id = d.Id,
                        Name = d.Name,
                        IsCompleted = completedItemIds.Contains(d.Id)
                    }).ToList(),
                    Quizzes = l.Quizzes.Select(q => new QuizContentDTO
                    {
                        Id = q.Id,
                        Name = q.Name,
                        IsCompleted = completedItemIds.Contains(q.Id)
                    }).ToList()
                }).ToList()
            };

            return new ApiResponse("Success", _localizer["Success"].Value, courseContent, true);
        }

        public async Task<ApiResponse> DeleteCourseAsync(string courseId, string instructorId)
        {
            try
            {
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == courseId);

                if (course == null)
                {
                    return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);
                }

                if (course.InstructorId != instructorId)
                {
                    return new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false);
                }

                if (!string.IsNullOrEmpty(course.ImagePublicId))
                {
                    await _cloudinaryService.DeleteImageAsync(course.ImagePublicId);
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                await RemoveRecommendedCache();
                await RemoveCourseDetailCache(courseId);

                try
                {
                    await _luceneSearchService.DeleteCourseFromIndexAsync(courseId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to remove course from index: {ex.Message}");
                }

                return new ApiResponse("Success", _localizer["DeleteCourseSuccess"].Value, null, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting course: {ex.Message}");
                return new ApiResponse("Error", _localizer["DeleteCourseFailed"].Value, null, false);
            }
        }

        public async Task<ApiResponse> CreateCourseRequestAsync(string courseId, string instructorId)
        {
            var course = await _context.Courses.Include(c => c.Instructor).FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null)
                return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);

            if (course.InstructorId != instructorId)
                return new ApiResponse("Unauthorized", _localizer["Unauthorized"].Value, null, false);

            if (course.Status == CourseStatus.Public)
                return new ApiResponse("Error", _localizer["CourseAlreadyPublic"].Value, null, false);

            var existingRequest = await _context.CourseRequests
                .FirstOrDefaultAsync(r => r.CourseId == courseId && r.Status == RequestStatus.Pending);

            if (existingRequest != null)
                return new ApiResponse("Error", _localizer["RequestAlreadySent"].Value, null, false);

            var request = new CourseRequest
            {
                CourseId = courseId,
                InstructorId = instructorId,
                Status = RequestStatus.Pending
            };

            _context.CourseRequests.Add(request);
            await _context.SaveChangesAsync();

            // Create notification for Admins
            await _notificationRepository.CreateNotificationForRoleAsync(
                NotificationRole.Admin,
                "New Course Approval Request",
                $"Instructor {course.Instructor.FullName} has submitted a new course: {course.Name}",
                NotificationType.CourseRequest
            );

            return new ApiResponse("Success", _localizer["RequestSentSuccess"].Value, null, true);
        }

        public async Task<ApiResponse> GetPendingCourseRequestsAsync()
        {
            var requests = await _context.CourseRequests
                .AsNoTracking()
                .Where(r => r.Status == RequestStatus.Pending)
                .Include(r => r.Course)
                    .ThenInclude(c => c.Instructor)
                .Select(r => new CourseRequestDTO
                {
                    Id = r.Id,
                    CourseId = r.CourseId,
                    CourseName = r.Course.Name,
                    InstructorId = r.InstructorId,
                    InstructorName = r.Course.Instructor.FullName,
                    CoursePrice = r.Course.Price,
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            if (!requests.Any())
                return new ApiResponse("Success", _localizer["NoData"].Value, null, true);

            return new ApiResponse("Success", _localizer["Success"].Value, requests, true);
        }

        public async Task<ApiResponse> ApproveCourseRequestAsync(string requestId, ResponseRequestDTO responseRequestDTO)
        {
            var request = await _context.CourseRequests
                .Include(r => r.Course)
                    .ThenInclude(c => c.Instructor)
                .Include(r => r.Course.CourseTags)
                .Include(r => r.Course.Enrollments)
                    .ThenInclude(e => e.Comments)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return new ApiResponse("NotFound", _localizer["RequestNotFound"].Value, null, false);

            if (request.Status != RequestStatus.Pending)
                return new ApiResponse("Error", _localizer["RequestNotPending"].Value, null, false);

            request.Status = RequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;

            if (request.Course != null)
            {
                request.Course.Status = CourseStatus.Public;
                request.Course.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            if (request.Course != null)
            {
                try
                {
                    await _luceneSearchService.IndexCourseAsync(request.Course);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Indexing failed: {ex.Message}");
                }
            }
            
            await RemoveRecommendedCache();
            await RemoveCourseDetailCache(request.CourseId);

            // Create notification for Instructor
            var notification = new Notification
            {
                UserId = request.InstructorId,
                Title = responseRequestDTO.Title,
                Message = responseRequestDTO.Message,
                Type = NotificationType.CourseRequestResult,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepository.CreateNotificationAsync(notification);

            return new ApiResponse("Success", _localizer["CourseRequestApproved"].Value, null, true);
        }

        public async Task<ApiResponse> RejectCourseRequestAsync(string requestId, ResponseRequestDTO responseRequestDTO)
        {
            var request = await _context.CourseRequests
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return new ApiResponse("NotFound", _localizer["RequestNotFound"].Value, null, false);

            if (request.Status != RequestStatus.Pending)
                return new ApiResponse("Error", _localizer["RequestNotPending"].Value, null, false);

            request.Status = RequestStatus.Rejected;
            request.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var notification = new Notification
            {
                UserId = request.InstructorId,
                Title = responseRequestDTO.Title,
                Message = responseRequestDTO.Message,
                Type = NotificationType.CourseRequestResult,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepository.CreateNotificationAsync(notification);

            return new ApiResponse("Success", _localizer["CourseRequestRejected"].Value, null, true);
        }

        public async Task<ApiResponse> GetAllCoursesForAdminAsync()
        {
            var courses = await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Comments)
                .Where(c => c.Status == CourseStatus.Public)
                .OrderByDescending(c => c.CreateTime)
                .ToListAsync();

            var courseDtos = courses.Select(c => new AdminCourseListDTO
            {
                Id = c.Id,
                Name = c.Name,
                ImageUrl = c.ImageUrl,
                InstructorName = c.Instructor.FullName,
                AverageRating = c.Enrollments.Any(e => e.Comments.Any(cm => cm.Type == CommentType.Review))
                                ? c.Enrollments.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).Average(cm => cm.Rate)
                                : 0,
                Price = c.Price,
                CreateTime = c.CreateTime,
                TotalStudents = c.Enrollments.Count
            }).ToList();

            return new ApiResponse("Success", _localizer["CoursesRetrieved"].Value, courseDtos, true);
        }

        public async Task<ApiResponse> AddCommentAsync(AddCommentDTO addCommentDTO, string userId)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == addCommentDTO.CourseId && e.StudentId == userId && e.Status == true);

            if (enrollment == null)
            {
                return new ApiResponse("Forbidden", _localizer["NotEnrolledInCourse"].Value, null, false);
            }

            if (addCommentDTO.Type == CommentType.Review)
            {
                var existingComment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == addCommentDTO.CourseId && c.Type == CommentType.Review);

                if (existingComment != null)
                {
                    return new ApiResponse("Conflict", _localizer["CommentAlreadyExists"].Value, null, false);
                }
            }

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

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            await RemoveCourseDetailCache(addCommentDTO.CourseId);

            // Re-index to update ratings
            try
            {
                var courseForIndex = await _context.Courses
                    .AsNoTracking()
                    .Include(c => c.Instructor)
                    .Include(c => c.CourseTags)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.Comments)
                    .FirstOrDefaultAsync(c => c.Id == addCommentDTO.CourseId);

                if (courseForIndex != null)
                {
                    await _luceneSearchService.IndexCourseAsync(courseForIndex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Indexing failed after comment: {ex.Message}");
            }

            // Trigger NewRating notification for instructor (smart: only if no unread exists)
            if (addCommentDTO.Type == CommentType.Review)
            {
                try
                {
                    var course = await _context.Courses.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == addCommentDTO.CourseId);
                    if (course != null)
                    {
                        var user = await _context.Users.AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Id == userId);
                        var hasUnread = await _context.Notifications
                            .AnyAsync(n => n.UserId == course.InstructorId
                                        && n.Type == NotificationType.NewRating
                                        && !n.IsRead);
                        if (!hasUnread)
                        {
                            await _notificationRepository.CreateNotificationAsync(new Notification
                            {
                                UserId = course.InstructorId,
                                Title = _localizer["NewRatingNotifTitle"].Value,
                                Message = string.Format(_localizer["NewRatingNotifMessage"].Value,
                                    user?.FullName ?? "Student", course.Name, addCommentDTO.Rate),
                                Type = NotificationType.NewRating,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"NewRating notification failed: {ex.Message}");
                }
            }

            return new ApiResponse("Created", _localizer["Success"].Value, comment.Id, true);
        }

        public async Task<ApiResponse> UpdateCommentAsync(string commentId, UpdateCommentDTO updateCommentDTO, string userId)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return new ApiResponse("NotFound", _localizer["CommentNotFound"].Value, null, false);
            }

            if (comment.UserId != userId)
            {
                return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            }

            comment.Content = updateCommentDTO.Content;
            if (comment.Type == CommentType.Review)
            {
                comment.Rate = updateCommentDTO.Rate;
            }
            comment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await RemoveCourseDetailCache(comment.CourseId);

            // Re-index to update ratings
            try
            {
                var courseForIndex = await _context.Courses
                    .AsNoTracking()
                    .Include(c => c.Instructor)
                    .Include(c => c.CourseTags)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.Comments)
                    .FirstOrDefaultAsync(c => c.Id == comment.CourseId);

                if (courseForIndex != null)
                {
                    await _luceneSearchService.IndexCourseAsync(courseForIndex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Indexing failed after comment update: {ex.Message}");
            }

            return new ApiResponse("Success", _localizer["CommentUpdated"].Value, null, true);
        }

        public async Task<ApiResponse> DeleteCommentAsync(string commentId, string userId)
        {
            var comment = await _context.Comments
                .Include(c => c.Course)
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return new ApiResponse("NotFound", _localizer["CommentNotFound"].Value, null, false);
            }

            // Instructor of the course OR the creator of the comment can delete
            if (comment.Course.InstructorId != userId && comment.UserId != userId)
            {
                return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            }

            if (comment.Replies.Any())
            {
                _context.Comments.RemoveRange(comment.Replies);
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            await RemoveCourseDetailCache(comment.CourseId);

            // Re-index to update ratings
            try
            {
                var courseForIndex = await _context.Courses
                    .AsNoTracking()
                    .Include(c => c.Instructor)
                    .Include(c => c.CourseTags)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.Comments)
                    .FirstOrDefaultAsync(c => c.Id == comment.CourseId);

                if (courseForIndex != null)
                {
                    await _luceneSearchService.IndexCourseAsync(courseForIndex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Indexing failed after comment deletion: {ex.Message}");
            }

            return new ApiResponse("Success", _localizer["CommentDeleted"].Value, null, true);
        }

        public async Task<ApiResponse> ReplyToCommentAsync(AddReplyCommentDTO replyDTO, string userId)
        {
            var parentComment = await _context.Comments
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Id == replyDTO.ParentCommentId);

            if (parentComment == null)
            {
                return new ApiResponse("NotFound", _localizer["ParentCommentNotFound"].Value, null, false);
            }

            // Only course instructor can reply
            if (parentComment.Course.InstructorId != userId)
            {
                return new ApiResponse("Forbidden", _localizer["ForbiddenReply"].Value, null, false);
            }

            var reply = new Comment
            {
                Id = Guid.NewGuid().ToString(),
                Content = replyDTO.Content,
                Rate = 0,
                UserId = userId,
                CourseId = parentComment.CourseId,
                ReplyId = parentComment.Id,
                CreatedAt = DateTime.UtcNow,
                Type = CommentType.Reply
            };

            _context.Comments.Add(reply);
            await _context.SaveChangesAsync();
            await RemoveCourseDetailCache(parentComment.CourseId);

            return new ApiResponse("Created", _localizer["ReplyAdded"].Value, null, true);
        }

        public async Task<ApiResponse> GetCourseQAThreadsAsync(string courseId, string userId, int pageNumber, int pageSize, string filter = "all")
        {
            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null)
            {
                return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);
            }

            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == courseId && e.StudentId == userId && e.Status == true);
            var isInstructor = course.InstructorId == userId;

            if (!isEnrolled && !isInstructor)
            {
                return new ApiResponse("Forbidden", _localizer["NotEnrolledInCourse"].Value, null, false);
            }

            var instructorId = course.InstructorId;

            var query = _context.QAThreads
                .AsNoTracking()
                .Where(t => t.CourseId == courseId)
                .Select(t => new 
                {
                    Thread = t,
                    LastMessageUserId = _context.QAMessages
                        .Where(m => m.ThreadId == t.Id)
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.UserId)
                        .FirstOrDefault()
                });

            // Unread logic: Last message is NOT from the course instructor
            // If no messages yet, check if creator is NOT instructor
            var threadDtosQuery = query.Select(x => new QAThreadDTO
            {
                Id = x.Thread.Id,
                Title = x.Thread.Title,
                CreatorName = x.Thread.Creator.FullName,
                CreatorAvatarUrl = x.Thread.Creator.AvatarUrl,
                CreatedAt = x.Thread.CreatedAt,
                LastActivityAt = x.Thread.LastActivityAt,
                IsMyThread = x.Thread.CreatorId == userId,
                TotalMessages = _context.QAMessages.Count(m => m.ThreadId == x.Thread.Id),
                IsUnread = x.LastMessageUserId != null 
                    ? x.LastMessageUserId != instructorId 
                    : x.Thread.CreatorId != instructorId
            });

            // Apply filter
            if (filter.ToLower() == "unread")
            {
                threadDtosQuery = threadDtosQuery.Where(t => t.IsUnread);
            }
            else if (filter.ToLower() == "read")
            {
                threadDtosQuery = threadDtosQuery.Where(t => !t.IsUnread);
            }

            var totalCount = await threadDtosQuery.CountAsync();

            var threads = await threadDtosQuery
                .OrderByDescending(t => t.IsUnread)
                .ThenByDescending(t => t.LastActivityAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedResult = new PagedResult<QAThreadDTO>
            {
                Items = threads,
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return new ApiResponse("Success", _localizer["Success"].Value, pagedResult, true);
        }

        public async Task<ApiResponse> GetThreadMessagesAsync(string threadId, string userId, int pageNumber, int pageSize)
        {
            var thread = await _context.QAThreads
                .Include(t => t.Creator)
                .Include(t => t.Course)
                .FirstOrDefaultAsync(t => t.Id == threadId);

            if (thread == null)
            {
                return new ApiResponse("NotFound", _localizer["QANotFound"].Value, null, false);
            }

            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == thread.CourseId && e.StudentId == userId && e.Status == true);
            var isInstructor = await _context.Courses.AnyAsync(c => c.Id == thread.CourseId && c.InstructorId == userId);

            if (!isEnrolled && !isInstructor)
            {
                return new ApiResponse("Forbidden", _localizer["NotEnrolledInCourse"].Value, null, false);
            }

            var query = _context.QAMessages
                .AsNoTracking()
                .Where(m => m.ThreadId == threadId);

            var totalCount = await query.CountAsync();

            var messages = await query
                .Include(m => m.User)
                .OrderBy(m => m.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new QAMessageDTO
                {
                    Id = m.Id,
                    Content = m.Content,
                    UserName = m.User.FullName,
                    AvatarUrl = m.User.AvatarUrl,
                    CreatedAt = m.CreatedAt,
                    IsMyMessage = m.UserId == userId,
                    IsInstructor = m.UserId == thread.Course.InstructorId
                })
                .ToListAsync();

            var threadDetail = new QAThreadDetailDTO
            {
                Id = thread.Id,
                Title = thread.Title,
                CreatorName = thread.Creator.FullName,
                CreatorAvatarUrl = thread.Creator.AvatarUrl,
                CreatedAt = thread.CreatedAt,
                IsMyThread = thread.CreatorId == userId
            };

            var pagedResult = new PagedResult<QAMessageDTO>
            {
                Items = messages,
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return new ApiResponse("Success", _localizer["Success"].Value, new { thread = threadDetail, messages = pagedResult }, true);
        }

        public async Task<ApiResponse> CreateQAThreadAsync(CreateThreadDTO createThreadDTO, string userId)
        {
            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == createThreadDTO.CourseId && e.StudentId == userId && e.Status == true);
            var isInstructor = await _context.Courses.AnyAsync(c => c.Id == createThreadDTO.CourseId && c.InstructorId == userId);

            if (!isEnrolled && !isInstructor)
            {
                return new ApiResponse("Forbidden", _localizer["NotEnrolledInCourse"].Value, null, false);
            }

            var thread = new QAThread
            {
                CourseId = createThreadDTO.CourseId,
                CreatorId = userId,
                Title = createThreadDTO.Title
            };

            _context.QAThreads.Add(thread);
            await _context.SaveChangesAsync();

            // Trigger NewQAQuestion notification for instructor (smart: only if no unread exists)
            if (!isInstructor)
            {
                try
                {
                    var course = await _context.Courses.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == createThreadDTO.CourseId);
                    if (course != null)
                    {
                        var user = await _context.Users.AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Id == userId);
                        var hasUnread = await _context.Notifications
                            .AnyAsync(n => n.UserId == course.InstructorId
                                        && n.Type == NotificationType.NewQAQuestion
                                        && !n.IsRead);
                        if (!hasUnread)
                        {
                            await _notificationRepository.CreateNotificationAsync(new Notification
                            {
                                UserId = course.InstructorId,
                                Title = _localizer["NewQAQuestionNotifTitle"].Value,
                                Message = string.Format(_localizer["NewQAQuestionNotifMessage"].Value,
                                    user?.FullName ?? "Student", course.Name, createThreadDTO.Title),
                                Type = NotificationType.NewQAQuestion,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"NewQAQuestion notification failed: {ex.Message}");
                }
            }

            return new ApiResponse("Created", _localizer["QuestionAdded"].Value, thread.Id, true);
        }

        public async Task<ApiResponse> AddMessageToThreadAsync(AddMessageDTO addMessageDTO, string userId)
        {
            var thread = await _context.QAThreads.FirstOrDefaultAsync(t => t.Id == addMessageDTO.ThreadId);
            if (thread == null)
            {
                return new ApiResponse("NotFound", _localizer["QANotFound"].Value, null, false);
            }

            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == thread.CourseId && e.StudentId == userId && e.Status == true);
            var isInstructor = await _context.Courses.AnyAsync(c => c.Id == thread.CourseId && c.InstructorId == userId);

            if (!isEnrolled && !isInstructor)
            {
                return new ApiResponse("Forbidden", _localizer["NotEnrolledInCourse"].Value, null, false);
            }

            var message = new QAMessage
            {
                ThreadId = thread.Id,
                UserId = userId,
                Content = addMessageDTO.Content
            };

            thread.LastActivityAt = DateTime.UtcNow;
            _context.QAMessages.Add(message);
            await _context.SaveChangesAsync();

            return new ApiResponse("Created", _localizer["ReplyAdded"].Value, message.Id, true);
        }

        public async Task<ApiResponse> UpdateQAThreadAsync(string threadId, UpdateThreadDTO updateThreadDTO, string userId)
        {
            var thread = await _context.QAThreads.FirstOrDefaultAsync(t => t.Id == threadId);
            if (thread == null) return new ApiResponse("NotFound", _localizer["QANotFound"].Value, null, false);

            if (thread.CreatorId != userId)
            {
                return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            }

            thread.Title = updateThreadDTO.Title;
            await _context.SaveChangesAsync();
            return new ApiResponse("Success", _localizer["QAUpdated"].Value, null, true);
        }

        public async Task<ApiResponse> UpdateQAMessageAsync(string messageId, UpdateMessageDTO updateMessageDTO, string userId)
        {
            var message = await _context.QAMessages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null) return new ApiResponse("NotFound", _localizer["QANotFound"].Value, null, false);

            if (message.UserId != userId)
            {
                return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            }

            message.Content = updateMessageDTO.Content;
            message.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return new ApiResponse("Success", _localizer["QAUpdated"].Value, null, true);
        }

        public async Task<ApiResponse> DeleteQAThreadAsync(string threadId, string userId)
        {
            var thread = await _context.QAThreads
                .Include(t => t.Course)
                .FirstOrDefaultAsync(t => t.Id == threadId);
                
            if (thread == null) return new ApiResponse("NotFound", _localizer["QANotFound"].Value, null, false);

            // Creator or Instructor can delete the entire thread
            if (thread.CreatorId != userId && thread.Course.InstructorId != userId)
            {
                return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            }

            _context.QAThreads.Remove(thread);
            await _context.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["QADeleted"].Value, null, true);
        }

        public async Task<ApiResponse> DeleteQAMessageAsync(string messageId, string userId)
        {
            var message = await _context.QAMessages
                .Include(m => m.Thread)
                    .ThenInclude(t => t.Course)
                .FirstOrDefaultAsync(m => m.Id == messageId);
                
            if (message == null) return new ApiResponse("NotFound", _localizer["QANotFound"].Value, null, false);

            // Message creator or Course Instructor can delete a message
            if (message.UserId != userId && message.Thread.Course.InstructorId != userId)
            {
                return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            }

            _context.QAMessages.Remove(message);
            await _context.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["QADeleted"].Value, null, true);
        }

        private async Task RemoveRecommendedCache()
        {
            await _cache.RemoveAsync("course:recommended:guest");
        }

        private async Task RemoveCourseDetailCache(string courseId)
        {
            // At least remove the guest view. Student-specific views will expire naturally
            // unless we use a library that supports tag-based or pattern-based invalidation.
            await _cache.RemoveAsync($"course:detail:{courseId}:guest");
        }


        public async Task<ApiResponse> MarkItemCompletedAsync(MarkItemCompletedDTO dto, string studentId)
        {
            try
            {
                var lecture = await _context.Lectures.AsNoTracking().FirstOrDefaultAsync(l => l.Id == dto.LectureId);
                if (lecture == null)
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

                var progress = await _context.StudentLectureProgresses
                    .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LectureId == dto.LectureId && p.ItemId == dto.ItemId && p.ItemType == dto.ItemType);

                if (progress == null)
                {
                    progress = new StudentLectureProgress
                    {
                        StudentId = studentId,
                        LectureId = dto.LectureId,
                        CourseId = lecture.CourseId,
                        ItemId = dto.ItemId,
                        ItemType = dto.ItemType,
                        IsCompleted = true
                    };
                    _context.StudentLectureProgresses.Add(progress);
                }
                else if (!progress.IsCompleted)
                {
                    progress.IsCompleted = true;
                }

                // Update LastVisit in Enrollment
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == lecture.CourseId);

                if (enrollment != null)
                {
                    enrollment.LastVisit = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return new ApiResponse("Success", _localizer["Success"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> UnmarkItemCompletedAsync(MarkItemCompletedDTO dto, string studentId)
        {
            try
            {
                var progress = await _context.StudentLectureProgresses
                    .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LectureId == dto.LectureId && p.ItemId == dto.ItemId && p.ItemType == dto.ItemType);

                if (progress != null)
                {
                    progress.IsCompleted = false;
                    await _context.SaveChangesAsync();
                }

                return new ApiResponse("Success", _localizer["Success"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> AddToWishlistAsync(string courseId, string studentId)
        {
            try
            {
                var exists = await _context.Wishlists.AnyAsync(w => w.StudentId == studentId && w.CourseId == courseId);
                if (exists)
                {
                    return new ApiResponse("Conflict", _localizer["AlreadyInWishlist"].Value, null, false);
                }

                var wishlist = new Wishlist
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = studentId,
                    CourseId = courseId,
                    AddedAt = DateTime.UtcNow
                };

                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();

                // If item is in cart, remove it
                string cacheKey = $"cart:{studentId}";
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(cachedData, JsonSettings.CamelCase);
                    if (apiResponse != null && apiResponse.Data != null)
                    {
                        CartDTO? cartDto = null;
                        if (apiResponse.Data is Newtonsoft.Json.Linq.JObject jObject)
                        {
                            cartDto = jObject.ToObject<CartDTO>();
                        }
                        else if (apiResponse.Data is CartDTO dto)
                        {
                            cartDto = dto;
                        }

                        if (cartDto != null && cartDto.Items.Any(i => i.Id == courseId))
                        {
                            cartDto.Items.RemoveAll(i => i.Id == courseId);
                            cartDto.TotalItems = cartDto.Items.Count;
                            cartDto.TotalPrice = cartDto.Items.Sum(i => i.Price);

                            var updateResponse = new ApiResponse("Success", _localizer["Success"].Value, cartDto, true);
                            var cacheOptions = new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                            };
                            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(updateResponse, JsonSettings.CamelCase), cacheOptions);
                        }
                    }
                }

                return new ApiResponse("Created", _localizer["AddedToWishlist"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> RemoveFromWishlistAsync(string courseId, string studentId)
        {
            try
            {
                var wishlist = await _context.Wishlists.FirstOrDefaultAsync(w => w.StudentId == studentId && w.CourseId == courseId);
                if (wishlist == null)
                {
                    return new ApiResponse("NotFound", _localizer["NotInWishlist"].Value, null, false);
                }

                _context.Wishlists.Remove(wishlist);
                await _context.SaveChangesAsync();

                return new ApiResponse("Success", _localizer["RemovedFromWishlist"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> GetStudentWishlistAsync(string studentId, int pageNumber, int pageSize)
        {
            try
            {
                var query = _context.Wishlists
                    .AsNoTracking()
                    .Where(w => w.StudentId == studentId);

                var totalCount = await query.CountAsync();

                var wishlistItems = await query
                    .Include(w => w.Course)
                        .ThenInclude(c => c.Instructor)
                    .Include(w => w.Course)
                        .ThenInclude(c => c.Enrollments)
                            .ThenInclude(e => e.Comments)
                    .Include(w => w.Course)
                        .ThenInclude(c => c.Lectures)
                            .ThenInclude(l => l.LectureVideos)
                    .OrderByDescending(w => w.AddedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var courseDTOs = wishlistItems.Select(w =>
                {
                    var course = w.Course;
                    var reviewComments = course.Enrollments
                                        .SelectMany(e => e.Comments)
                                        .Where(cm => cm.Type == CommentType.Review)
                                        .ToList();
                    
                    var avgRating = reviewComments.Any() ? reviewComments.Average(cm => cm.Rate) : 0;
                    var calculatedPrice = CalculatePrice(course);

                    var dto = new CourseCardDTO
                    {
                        Id = course.Id,
                        Name = course.Name,
                        Description = course.Description,
                        ImageUrl = course.ImageUrl,
                        InstructorName = course.Instructor.FullName,
                        AverageRating = Math.Round(avgRating, 1),
                        TotalReviews = reviewComments.Count,
                        TotalStudents = course.Enrollments.Count,
                        OriginalPrice = course.Price,
                        Price = calculatedPrice,
                        IsBestseller = course.Enrollments.Count > 5,
                        TotalHours = course.Lectures.SelectMany(l => l.LectureVideos).Any()
                                     ? (int)Math.Max(1, Math.Round(course.Lectures.SelectMany(l => l.LectureVideos).Sum(v => v.Duration) / 3600.0))
                                     : 0,
                        LastUpdate = course.UpdatedAt == default ? course.CreateTime : course.UpdatedAt
                    };

                    if (dto.Price == dto.OriginalPrice) dto.OriginalPrice = null;
                    return dto;
                }).ToList();

                var pagedResult = new PagedResult<CourseCardDTO>
                {
                    Items = courseDTOs,
                    Page = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };

                return new ApiResponse("Success", _localizer["Success"].Value, pagedResult, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        private decimal CalculatePrice(Course course)
        {
            if (course == null) return 0m;
            if (string.IsNullOrEmpty(course.Id)) return course.Price;
            try
            {
                var hexChar = course.Id.Substring(0, 1);
                var value = int.Parse(hexChar, System.Globalization.NumberStyles.HexNumber);
                return (value % 2 != 0) ? (course.Price * 0.5m) : course.Price;
            }
            catch
            {
                return course.Price;
            }
        }

        public async Task<ApiResponse> GetInstructorDashboardAsync(string instructorId)
        {
            try
            {
                var courses = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.InstructorId == instructorId)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.Comments)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.Student)
                    .ToListAsync();

                var courseIds = courses.Select(c => c.Id).ToList();

                // 4 Overview Cards
                var totalStudents = courses.Sum(c => c.Enrollments.Count);
                var totalRevenue = courses.Sum(c => (long)c.Price * c.Enrollments.Count);
                var allReviewComments = courses
                    .SelectMany(c => c.Enrollments.SelectMany(e => e.Comments))
                    .Where(cm => cm.Type == CommentType.Review && cm.Rate >= 1)
                    .ToList();
                var averageRating = allReviewComments.Any() ? allReviewComments.Average(cm => cm.Rate) : 0;

                // Enrollment Chart (30 days)
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var enrollmentChartData = courses
                    .SelectMany(c => c.Enrollments)
                    .Where(e => e.EnrolledAt >= thirtyDaysAgo)
                    .GroupBy(e => e.EnrolledAt.Date)
                    .Select(g => new DailyEnrollmentDTO
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Count = g.Count()
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                // Fill in missing days with 0
                var allDays = Enumerable.Range(0, 30)
                    .Select(i => DateTime.UtcNow.AddDays(-29 + i).Date.ToString("yyyy-MM-dd"))
                    .ToList();
                var enrollmentChart = allDays.Select(day => new DailyEnrollmentDTO
                {
                    Date = day,
                    Count = enrollmentChartData.FirstOrDefault(d => d.Date == day)?.Count ?? 0
                }).ToList();

                // Rating Distribution (1-5 stars)
                var ratingDistribution = Enumerable.Range(1, 5)
                    .Select(star => new RatingDistributionDTO
                    {
                        Star = star,
                        Count = allReviewComments.Count(cm => cm.Rate == star)
                    })
                    .ToList();

                var dashboard = new InstructorDashboardDTO
                {
                    TotalStudents = totalStudents,
                    TotalRevenue = totalRevenue,
                    AverageRating = Math.Round(averageRating, 1),
                    TotalCourses = courses.Count,
                    EnrollmentChart = enrollmentChart,
                    RatingDistribution = ratingDistribution
                };

                return new ApiResponse("Success", _localizer["DashboardRetrieved"].Value, dashboard, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }

        public async Task<ApiResponse> GetInstructorActivitiesAsync(string instructorId, int page, int pageSize)
        {
            try
            {
                var courseIds = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.InstructorId == instructorId)
                    .Select(c => c.Id)
                    .ToListAsync();

                // Build three queries returning identical projections
                var enrollmentsQuery = _context.Enrollments
                    .AsNoTracking()
                    .Where(e => courseIds.Contains(e.CourseId))
                    .Select(e => new RecentActivityDTO
                    {
                        Type = "enrollment",
                        CourseName = e.Course.Name,
                        StudentName = e.Student.FullName ?? "Student",
                        Rating = 0,
                        QuestionTitle = "",
                        CreatedAt = e.EnrolledAt
                    });

                var ratingsQuery = _context.Comments
                    .AsNoTracking()
                    .Where(c => courseIds.Contains(c.CourseId) && c.Type == CommentType.Review && c.Rate >= 1)
                    .Select(c => new RecentActivityDTO
                    {
                        Type = "rating",
                        CourseName = c.Course.Name,
                        StudentName = c.User.FullName ?? "Student",
                        Rating = c.Rate,
                        QuestionTitle = "",
                        CreatedAt = c.CreatedAt
                    });

                var qaQuery = _context.QAThreads
                    .AsNoTracking()
                    .Where(t => courseIds.Contains(t.CourseId))
                    .Select(t => new RecentActivityDTO
                    {
                        Type = "qa_question",
                        CourseName = t.Course.Name,
                        StudentName = t.Creator.FullName,
                        Rating = 0,
                        QuestionTitle = t.Title,
                        CreatedAt = t.CreatedAt
                    });

                // Combine them
                var combinedQuery = enrollmentsQuery.Union(ratingsQuery).Union(qaQuery);

                var totalCount = await combinedQuery.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var items = await combinedQuery
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var pagedResult = new PagedResult<RecentActivityDTO>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };

                return new ApiResponse("Success", _localizer["ActivitiesRetrieved"].Value, pagedResult, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
        public async Task<ApiResponse> GetInstructorUnreadThreadsAsync(string instructorId)
        {
            try
            {
                var courseIds = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.InstructorId == instructorId)
                    .Select(c => c.Id)
                    .ToListAsync();

                var threads = await _context.QAThreads
                    .AsNoTracking()
                    .Where(t => courseIds.Contains(t.CourseId))
                    .Include(t => t.Messages)
                    .Include(t => t.Course)
                    .ToListAsync();

                // Thread is "unread" if last message is NOT from instructor
                var unreadByCourse = threads
                    .Where(t =>
                    {
                        var lastMessage = t.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
                        // Unread if: has messages and last message is not from instructor, OR has no messages (new thread from student)
                        return lastMessage != null ? lastMessage.UserId != instructorId : t.CreatorId != instructorId;
                    })
                    .GroupBy(t => t.CourseId)
                    .Select(g => new UnreadThreadCourseDTO
                    {
                        CourseId = g.Key,
                        CourseName = g.First().Course.Name,
                        CourseImage = g.First().Course.ImageUrl,
                        UnreadThreadCount = g.Count(),
                        LastActivityAt = g.Max(t => t.LastActivityAt)
                    })
                    .OrderByDescending(x => x.LastActivityAt)
                    .ToList();

                return new ApiResponse("Success", _localizer["UnreadThreadsRetrieved"].Value, unreadByCourse, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
    }
}