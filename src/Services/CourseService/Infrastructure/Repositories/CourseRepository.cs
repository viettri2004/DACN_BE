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

        public async Task<ApiResponse> GetCoursesAsync(CourseQueryParameters queryParams, string studentId)
        {
            var query = _context.Courses.AsNoTracking()
                .Where(c => c.Status != CourseStatus.Private);

            if (queryParams.TagIds != null && queryParams.TagIds.Any())
            {
                query = query.Where(c => c.CourseTags.Any(ct => queryParams.TagIds.Contains(ct.TagId)));
            }

            var allCoursesList = await query
                .Include(c => c.Instructor)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Comments)
                .ToListAsync();

            if (!string.IsNullOrEmpty(studentId))
            {
                var enrolledCourseIds = await _context.Enrollments
                    .Where(e => e.StudentId == studentId && e.Status == true)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                allCoursesList = allCoursesList.Where(c => !enrolledCourseIds.Contains(c.Id)).ToList();
            }

            var coursesWithPrice = allCoursesList.Select(c =>
            {
                var calculatedPrice = (int.Parse(c.Id.Substring(0, 1), System.Globalization.NumberStyles.HexNumber) % 2 != 0)
                                    ? (c.Price * 0.5m)
                                    : c.Price;

                return new
                {
                    Course = c,
                    Price = calculatedPrice
                };
            }).AsQueryable();

            if (queryParams.MinPrice.HasValue)
            {
                coursesWithPrice = coursesWithPrice.Where(x => x.Price >= queryParams.MinPrice.Value);
            }

            if (queryParams.MaxPrice.HasValue)
            {
                coursesWithPrice = coursesWithPrice.Where(x => x.Price <= queryParams.MaxPrice.Value);
            }

            var totalCount = coursesWithPrice.Count();

            switch (queryParams.SortBy?.ToLower())
            {
                case "rating":
                    coursesWithPrice = coursesWithPrice.OrderByDescending(x => x.Course.Enrollments.SelectMany(e => e.Comments).Any(cm => cm.Type == CommentType.Review)
                                                    ? x.Course.Enrollments.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).Average(cm => cm.Rate)
                                                    : 0);
                    break;
                case "newest":
                    coursesWithPrice = coursesWithPrice.OrderByDescending(x => x.Course.CreateTime);
                    break;
                case "priceasc":
                    coursesWithPrice = coursesWithPrice.OrderBy(x => x.Price);
                    break;
                case "pricedesc":
                    coursesWithPrice = coursesWithPrice.OrderByDescending(x => x.Price);
                    break;
                case "popularity":
                default:
                    coursesWithPrice = coursesWithPrice.OrderByDescending(x => x.Course.Enrollments.Count);
                    break;
            }

            var pagedData = coursesWithPrice
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToList();

            var pagedCoursesData = pagedData.Select(x => new CourseCardDTO
            {
                Id = x.Course.Id,
                Name = x.Course.Name,
                ImageUrl = x.Course.ImageUrl,
                InstructorName = x.Course.Instructor.FullName,
                AverageRating = x.Course.Enrollments.SelectMany(e => e.Comments).Any(cm => cm.Type == CommentType.Review)
                                     ? Math.Round(x.Course.Enrollments.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).Average(cm => cm.Rate), 1)
                                     : 0,
                TotalReviews = x.Course.Enrollments.SelectMany(e => e.Comments).Count(cm => cm.Type == CommentType.Review),
                TotalStudents = x.Course.Enrollments.Count,
                OriginalPrice = x.Course.Price,
                Price = x.Price,
                IsBestseller = x.Course.Enrollments.Count > 5,
                TotalHours = 25,
                // Status = x.Course.Status.ToString()
            }).ToList();

            foreach (var course in pagedCoursesData)
            {
                if (course.Price == course.OriginalPrice)
                {
                    course.OriginalPrice = null;
                }
            }

            var pagedResult = new PagedResult<CourseCardDTO>
            {
                Items = pagedCoursesData,
                Page = queryParams.Page,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount
            };

            return new ApiResponse("Success", _localizer["Success"].Value, pagedResult, true);
        }
        public async Task<ApiResponse> CreateCourseAsync(CreateCourseDTO createCourseDTO, string instructorId)
        {
            try
            {
                string imageUrl = string.Empty;
                string imagePublicId = string.Empty;
                if (createCourseDTO.image != null)
                {
                    (imageUrl, imagePublicId) = await _cloudinaryService.UploadImageAsync(createCourseDTO.image);
                }

                var newCourse = new Course
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = createCourseDTO.name,
                    Price = createCourseDTO.price,
                    Description = createCourseDTO.description ?? string.Empty,
                    ImageUrl = imageUrl,
                    ImagePublicId = imagePublicId,
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
                    null,
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

                if (updateCourseDTO.image != null)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(course.ImagePublicId))
                    {
                        await _cloudinaryService.DeleteImageAsync(course.ImagePublicId);
                    }

                    // Upload new image
                    var (imageUrl, imagePublicId) = await _cloudinaryService.UploadImageAsync(updateCourseDTO.image);
                    course.ImageUrl = imageUrl;
                    course.ImagePublicId = imagePublicId;
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

        public async Task<ApiResponse> GetCourseCommentsAsync(string courseId, string? userId, CommentType type)
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

            var allComments = await commentQuery
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDTO
                {
                    CommentId = c.Id,
                    UserName = c.User.FullName,
                    AvatarUrl = c.User.AvatarUrl,
                    Rate = c.Rate,
                    Content = c.Content,
                    Type = c.Type,
                    IsMyComment = userId != null && c.UserId == userId,
                    CanDelete = userId != null && course.InstructorId == userId,
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
                        CanDelete = userId != null && course.InstructorId == userId
                    }).ToList()
                })
                .ToListAsync();

            var response = new CourseCommentsResponseDTO
            {
                IsInstructor = isInstructor,
                MyComment = allComments.FirstOrDefault(c => c.IsMyComment && c.Type == CommentType.Review),
                AllComments = allComments.Where(c => !(c.IsMyComment && c.Type == CommentType.Review)).ToList()
            };

            return new ApiResponse("Success", _localizer["Success"].Value, response, true);
        }

        public async Task<ApiResponse> GetRecommendedCoursesAsync()
        {
            string cacheKey = "course:recommended";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<ApiResponse>(cachedData, JsonSettings.CamelCase);
            }

            var courseDTOs = await _context.Courses
                .AsNoTracking()
                .OrderByDescending(c => c.Enrollments
                                .SelectMany(e => e.Comments)
                                .Where(cm => cm.Type == CommentType.Review)
                                .Average(cm => (double?)cm.Rate) ?? 0)
                .Take(3)
                .Select(c => new CourseListDTO
                {
                    Id = c.Id,
                    ImageUrl = c.ImageUrl,
                    Name = c.Name,
                    InstructorName = c.Instructor.FullName,
                    Rating = c.Enrollments
                                .SelectMany(e => e.Comments)
                                .Where(cm => cm.Type == CommentType.Review)
                                .Average(cm => (double?)cm.Rate) ?? 0
                            ,
                    Price = c.Price,
                    // Status = c.Status.ToString()
                })
                .Take(5)
                .ToListAsync();

            if (courseDTOs.Count == 0)
                return new ApiResponse("Success", _localizer["NoData"].Value, null, true);

            var response = new ApiResponse("Success", _localizer["Success"].Value, courseDTOs, true);

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

            var totalInstructorCourses = await _context.Courses
                .CountAsync(c => c.InstructorId == course.InstructorId);

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
                Tags = course.CourseTags?.Select(t => t.Tag.Name).ToList() ?? new List<string>(),
                InstructorName = course.Instructor.FullName,
                InstructorJobPosition = course.Instructor.JobPosition ?? _localizer["DefaultInstructorJobPosition"].Value,
                InstructorTotalCourses = totalInstructorCourses,
                Rating = course.Enrollments.SelectMany(e => e.Comments).Any(cm => cm.Type == CommentType.Review)
                        ? course.Enrollments.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).Average(cm => cm.Rate)
                        : 0,
                TotalReviews = course.Enrollments.SelectMany(e => e.Comments).Count(cm => cm.Type == CommentType.Review),
                TotalStudents = course.Enrollments.Count,
                TotalHours = totalSeconds > 0 ? Math.Max(0.1, Math.Round(totalHours, 2)) : 0,
                UpdatedAt = course.UpdatedAt == default ? course.CreateTime : course.UpdatedAt,
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

                // Enqueue AI processing jobs for each video using Hangfire
                var videoIds = await _context.LectureVideos
                    .Where(v => v.Lecture.CourseId == request.CourseId)
                    .Select(v => v.Id)
                    .ToListAsync();

                foreach (var videoId in videoIds)
                {
                    _backgroundJobClient.Enqueue<IVideoProcessingService>(x => x.ProcessVideoAsync(videoId));
                }

                try
                {
                    await _luceneSearchService.IndexCourseAsync(request.Course);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Indexing failed: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
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

            return new ApiResponse("Created", _localizer["Success"].Value, null, true);
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

            // Instructor of the course can delete any comment
            if (comment.Course.InstructorId != userId)
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

        public async Task<ApiResponse> GetCourseQAsAsync(string courseId, string userId)
        {
            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == courseId && e.StudentId == userId && e.Status == true);
            var isInstructor = await _context.Courses.AnyAsync(c => c.Id == courseId && c.InstructorId == userId);

            if (!isEnrolled && !isInstructor)
            {
                return new ApiResponse("Forbidden", _localizer["NotEnrolledInCourse"].Value, null, false);
            }

            var qas = await _context.QuestionAnswers
                .Include(qa => qa.User)
                .Include(qa => qa.Replies).ThenInclude(r => r.User)
                .Where(qa => qa.CourseId == courseId && qa.ParentId == null)
                .OrderByDescending(qa => qa.CreatedAt)
                .Select(qa => new QuestionAnswerDTO
                {
                    Id = qa.Id,
                    Title = qa.Title,
                    Content = qa.Content,
                    UserName = qa.User.FullName,
                    AvatarUrl = qa.User.AvatarUrl,
                    CreatedAt = qa.CreatedAt,
                    IsMyQA = qa.UserId == userId,
                    Replies = qa.Replies.OrderBy(r => r.CreatedAt).Select(r => new QuestionAnswerDTO
                    {
                        Id = r.Id,
                        Content = r.Content,
                        UserName = r.User.FullName,
                        AvatarUrl = r.User.AvatarUrl,
                        CreatedAt = r.CreatedAt,
                        IsMyQA = r.UserId == userId
                    }).ToList()
                })
                .ToListAsync();

            return new ApiResponse("Success", _localizer["Success"].Value, qas, true);
        }

        public async Task<ApiResponse> CreateQuestionAsync(CreateQuestionDTO createQuestionDTO, string userId)
        {
            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == createQuestionDTO.CourseId && e.StudentId == userId && e.Status == true);
            var isInstructor = await _context.Courses.AnyAsync(c => c.Id == createQuestionDTO.CourseId && c.InstructorId == userId);

            if (!isEnrolled && !isInstructor)
            {
                return new ApiResponse("Forbidden", _localizer["NotEnrolledInCourse"].Value, null, false);
            }

            var qa = new QuestionAnswer
            {
                CourseId = createQuestionDTO.CourseId,
                UserId = userId,
                Title = createQuestionDTO.Title,
                Content = createQuestionDTO.Content
            };

            _context.QuestionAnswers.Add(qa);
            await _context.SaveChangesAsync();

            return new ApiResponse("Created", _localizer["QuestionAdded"].Value, null, true);
        }

        public async Task<ApiResponse> ReplyToQAAsync(ReplyQADTO replyDTO, string userId)
        {
            var parent = await _context.QuestionAnswers
                .FirstOrDefaultAsync(qa => qa.Id == replyDTO.ParentId);

            if (parent == null)
            {
                return new ApiResponse("NotFound", _localizer["QANotFound"].Value, null, false);
            }

            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == parent.CourseId && e.StudentId == userId && e.Status == true);
            var isInstructor = await _context.Courses.AnyAsync(c => c.Id == parent.CourseId && c.InstructorId == userId);

            if (!isEnrolled && !isInstructor)
            {
                return new ApiResponse("Forbidden", _localizer["NotEnrolledInCourse"].Value, null, false);
            }

            var reply = new QuestionAnswer
            {
                CourseId = parent.CourseId,
                UserId = userId,
                ParentId = parent.Id,
                Content = replyDTO.Content
            };

            _context.QuestionAnswers.Add(reply);
            await _context.SaveChangesAsync();

            return new ApiResponse("Created", _localizer["ReplyAdded"].Value, null, true);
        }

        public async Task<ApiResponse> UpdateQAAsync(string qaId, UpdateQADTO updateQADTO, string userId)
        {
            var qa = await _context.QuestionAnswers.FirstOrDefaultAsync(qa => qa.Id == qaId);
            if (qa == null) return new ApiResponse("NotFound", _localizer["QANotFound"].Value, null, false);

            if (qa.UserId != userId)
            {
                return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            }

            qa.Title = updateQADTO.Title;
            qa.Content = updateQADTO.Content;
            qa.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return new ApiResponse("Success", _localizer["QAUpdated"].Value, null, true);
        }

        public async Task<ApiResponse> DeleteQAAsync(string qaId, string userId)
        {
            var qa = await _context.QuestionAnswers
                .Include(q => q.Course)
                .Include(q => q.Replies)
                .FirstOrDefaultAsync(q => q.Id == qaId);
                
            if (qa == null) return new ApiResponse("NotFound", _localizer["QANotFound"].Value, null, false);

            // Owner or Instructor can delete
            if (qa.UserId != userId && qa.Course.InstructorId != userId)
            {
                return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            }

            if (qa.Replies.Any())
            {
                _context.QuestionAnswers.RemoveRange(qa.Replies);
            }
            _context.QuestionAnswers.Remove(qa);
            await _context.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["QADeleted"].Value, null, true);
        }

        private async Task RemoveRecommendedCache()
        {
            await _cache.RemoveAsync("course:recommended");
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
    }
}
