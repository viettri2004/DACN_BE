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

            var totalHours = course.Lectures
                .SelectMany(l => l.LectureVideos)
                .Sum(lv => lv.Duration) / 3600.0;

            var allVideos = course.Lectures
                .OrderBy(l => l.DisplayOrder)
                .SelectMany(l => l.LectureVideos.OrderBy(lv => lv.DisplayOrder))
                .ToList();

            int progressPercentage = 0;
            if (isEnrolled && course.Lectures.Count > 0)
            {
                var completedLectures = await _context.StudentLectureProgresses
                    .CountAsync(p => p.CourseId == courseId && p.StudentId == studentId && p.IsCompleted);
                progressPercentage = (completedLectures * 100) / course.Lectures.Count;
            }

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
                TotalHours = Math.Round(totalHours, 1),
                IsEnrolled = isEnrolled,
                Level = course.Level ?? _localizer["DefaultCourseLevel"].Value,
                Access = course.Access ?? _localizer["DefaultCourseAccess"].Value,
                Language = course.Language ?? _localizer["DefaultCourseLanguage"].Value,
                UpdatedAt = course.UpdatedAt,
                Progress = progressPercentage,
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

        public async Task<ApiResponse> GetCourseCommentsAsync(string courseId, string? userId)
        {
            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null)
                return new ApiResponse("NotFound", _localizer["CourseNotFound"].Value, null, false);

            bool isInstructor = userId != null && course.InstructorId == userId;

            var allComments = await _context.Comments
                .AsNoTracking()
                .Include(c => c.Enrollment.Student)
                .Include(c => c.Enrollment.Course)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.Enrollment.Student)
                .Where(c => c.Enrollment.CourseId == courseId && c.ReplyId == null && c.Type == CommentType.Review)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDTO
                {
                    CommentId = c.Id,
                    UserName = c.Enrollment.Student.FullName,
                    AvatarUrl = c.Enrollment.Student.AvatarUrl,
                    Rate = c.Rate,
                    Content = c.Content,
                    IsMyComment = userId != null && c.Enrollment.StudentId == userId,
                    CanDelete = false,
                    Timestamp = c.CreatedAt,
                    Replies = c.Replies.Select(r => new ReplyDTO
                    {
                        CommentId = r.Id,
                        Content = r.Content,
                        Timestamp = r.CreatedAt,
                        IsMyComment = isInstructor,
                        CanDelete = isInstructor
                    }).OrderBy(r => r.Timestamp).ToList()
                })
                .ToListAsync();

            var response = new CourseCommentsResponseDTO
            {
                IsInstructor = isInstructor,
                MyComment = allComments.FirstOrDefault(c => c.IsMyComment),
                AllComments = allComments.Where(c => !c.IsMyComment).ToList()
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
                .Where(e => e.StudentId == studentId)
                .Select(e => new CourseListDTO
                {
                    Id = e.Course.Id,
                    ImageUrl = e.Course.ImageUrl,
                    Name = e.Course.Name,
                    InstructorName = e.Course.Instructor.FullName,
                    Rating = e.Course.Enrollments
                        .SelectMany(en => en.Comments)
                        .Where(cm => cm.Type == CommentType.Review)
                        .Any()
                            ? e.Course.Enrollments
                                .SelectMany(en => en.Comments)
                                .Where(cm => cm.Type == CommentType.Review)
                                .Average(cm => cm.Rate)
                            : 0,
                    Price = e.Order.OrderItems
                        .Where(oi => oi.CourseId == e.Course.Id)
                        .Sum(oi => (decimal?)oi.Price) ?? 0,
                    Progress = _context.Lectures.Count(l => l.CourseId == e.Course.Id) == 0 ? 0 :
                               (_context.StudentLectureProgresses.Count(p => p.CourseId == e.Course.Id && p.StudentId == studentId && p.IsCompleted) * 100) /
                               _context.Lectures.Count(l => l.CourseId == e.Course.Id)
                })
                .ToListAsync();

            if (courseDTOs.Count == 0)
                return new ApiResponse("Success", _localizer["NoData"].Value, null, true);

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
                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.CourseId == courseId && e.StudentId == userId && e.Status == true);

                if (isEnrolled)
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                return new ApiResponse("Forbidden", _localizer["ForbiddenCourseContent"].Value, null, false);
            }

            var courseContent = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == courseId)
                .Select(c => new CourseContentDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    // Status = c.Status.ToString(),
                    Tags = c.CourseTags.Select(t => t.Tag.Name).ToList(),
                    Lectures = c.Lectures
                    .OrderBy(l => l.DisplayOrder)
                    .Select(l => new LectureContentDTO
                    {
                        Id = l.Id,
                        Name = l.Name,
                        Description = l.Description,
                        DisplayOrder = l.DisplayOrder,
                        Videos = l.LectureVideos
                        .OrderBy(v => v.DisplayOrder)
                        .Select(v => new VideoContentDTO
                        {
                            Id = v.Id,
                            DisplayOrder = v.DisplayOrder,
                            Name = v.Name,
                            Duration = v.Duration
                        }).ToList(),
                        Documents = l.Documents.Select(d => new DocumentContentDTO
                        {
                            Id = d.Id,
                            Name = d.Name
                        }).ToList(),
                        Quizzes = l.Quizzes.Select(q => new QuizContentDTO
                        {
                            Id = q.Id,
                            Name = q.Name
                        }).ToList()
                    }).ToList()
                })
                .FirstOrDefaultAsync();

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
            var existingComment = await _context.Comments
                .FirstOrDefaultAsync(c => c.EnrollmentId == enrollment.Id && c.Type == CommentType.Review);

            if (existingComment != null)
            {
                return new ApiResponse("Conflict", _localizer["CommentAlreadyExists"].Value, null, false);
            }

            var comment = new Comment
            {
                Id = Guid.NewGuid().ToString(),
                Content = addCommentDTO.Content,
                Rate = addCommentDTO.Rate,
                EnrollmentId = enrollment.Id,
                CreatedAt = DateTime.UtcNow,
                Type = CommentType.Review
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            await RemoveCourseDetailCache(addCommentDTO.CourseId);

            return new ApiResponse("Created", _localizer["CommentAdded"].Value, null, true);
        }

        public async Task<ApiResponse> UpdateCommentAsync(string commentId, UpdateCommentDTO updateCommentDTO, string userId)
        {
            var comment = await _context.Comments
                .Include(c => c.Enrollment.Course)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return new ApiResponse("NotFound", _localizer["CommentNotFound"].Value, null, false);
            }

            // Student update their own comment OR Instructor update their own reply
            bool isOwner = (comment.Type == CommentType.Review && comment.Enrollment.StudentId == userId) ||
                          (comment.Type == CommentType.Reply && comment.Enrollment.Course.InstructorId == userId);

            if (!isOwner)
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
            await RemoveCourseDetailCache(comment.Enrollment.CourseId);

            return new ApiResponse("Success", _localizer["CommentUpdated"].Value, null, true);
        }

        public async Task<ApiResponse> DeleteCommentAsync(string commentId, string userId)
        {
            var comment = await _context.Comments
                .Include(c => c.Enrollment.Course)
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return new ApiResponse("NotFound", _localizer["CommentNotFound"].Value, null, false);
            }

            if (comment.Enrollment.Course.InstructorId != userId)
            {
                return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            }

            if (comment.Replies.Any())
            {
                _context.Comments.RemoveRange(comment.Replies);
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            await RemoveCourseDetailCache(comment.Enrollment.CourseId);

            return new ApiResponse("Success", _localizer["CommentDeleted"].Value, null, true);
        }

        public async Task<ApiResponse> ReplyToCommentAsync(AddReplyCommentDTO replyDTO, string userId)
        {
            var parentComment = await _context.Comments
                .Include(c => c.Enrollment.Course)
                .FirstOrDefaultAsync(c => c.Id == replyDTO.ParentCommentId);

            if (parentComment == null)
            {
                return new ApiResponse("NotFound", _localizer["ParentCommentNotFound"].Value, null, false);
            }

            // Only course instructor can reply
            if (parentComment.Enrollment.Course.InstructorId != userId)
            {
                return new ApiResponse("Forbidden", _localizer["ForbiddenReply"].Value, null, false);
            }

            // Use the parent's EnrollmentId since instructors don't have one
            var reply = new Comment
            {
                Id = Guid.NewGuid().ToString(),
                Content = replyDTO.Content,
                Rate = 0,
                EnrollmentId = parentComment.EnrollmentId,
                ReplyId = parentComment.Id,
                CreatedAt = DateTime.UtcNow,
                Type = CommentType.Reply
            };

            _context.Comments.Add(reply);
            await _context.SaveChangesAsync();
            await RemoveCourseDetailCache(parentComment.Enrollment.CourseId);

            return new ApiResponse("Created", _localizer["ReplyAdded"].Value, null, true);
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
        

        public async Task<ApiResponse> MarkLectureCompletedAsync(string lectureId, string studentId)
        {
            try
            {
                var lecture = await _context.Lectures.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lectureId);
                if (lecture == null)
                    return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

                var progress = await _context.StudentLectureProgresses
                    .FirstOrDefaultAsync(p => p.LectureId == lectureId && p.StudentId == studentId);

                if (progress == null)
                {
                    progress = new StudentLectureProgress
                    {
                        StudentId = studentId,
                        LectureId = lectureId,
                        CourseId = lecture.CourseId,
                        IsCompleted = true,
                        CompletedAt = DateTime.UtcNow
                    };
                    _context.StudentLectureProgresses.Add(progress);
                }
                else if (!progress.IsCompleted)
                {
                    progress.IsCompleted = true;
                    progress.CompletedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return new ApiResponse("Success", _localizer["Success"].Value, null, true);
            }
            catch (Exception ex)
            {
                return new ApiResponse("Error", ex.Message, null, false);
            }
        }
    }
}
