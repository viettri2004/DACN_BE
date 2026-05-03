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

                _backgroundJobClient.Enqueue(() => _luceneSearchService.IndexCourseAsync(newCourse.Id));

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
                _backgroundJobClient.Enqueue(() => _luceneSearchService.IndexCourseAsync(course.Id));

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
                .Take(pageSize == 10 ? 3 : pageSize)
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

        #endregion

        #region Student Features

        public async Task<ApiResponse> GetCoursesByStudentIdAsync(string studentId, int pageNumber, int pageSize)
        {
            var query = _courseRepository.GetEnrollmentsQueryable()
                .AsNoTracking()
                .Where(e => e.StudentId == studentId && e.Status == true);

            var totalCount = await query.CountAsync();

            // Optimizing: Fetch all progress for this student once
            var allProgress = await _courseRepository.GetProgressQueryable()
                .AsNoTracking()
                .Where(p => p.StudentId == studentId && p.IsCompleted == true)
                .GroupBy(p => p.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CourseId, x => x.Count);

            // Fetch ALL active enrollments with required details for overall stats
            var allEnrollments = await query
                .Include(e => e.Course).ThenInclude(c => c.Instructor)
                .Include(e => e.Course).ThenInclude(c => c.Lectures).ThenInclude(l => l.LectureVideos)
                .Include(e => e.Course).ThenInclude(c => c.Lectures).ThenInclude(l => l.Documents)
                .Include(e => e.Course).ThenInclude(c => c.Lectures).ThenInclude(l => l.Quizzes)
                .ToListAsync();

            int completedCourses = 0;
            double totalStudyTime = 0.0;
            double totalProgress = 0.0;

            foreach (var e in allEnrollments)
            {
                var tl = e.Course.Lectures.Sum(l => 
                    (l.LectureVideos?.Count ?? 0) + (l.Documents?.Count ?? 0) + (l.Quizzes?.Count ?? 0));

                allProgress.TryGetValue(e.CourseId, out int cl);

                var p = tl > 0 ? (int)Math.Round((double)cl / tl * 100) : 0;
                if (p == 100) completedCourses++;

                var totalDuration = e.Course.Lectures.SelectMany(l => l.LectureVideos).Sum(v => v.Duration);
                var hours = Math.Round(totalDuration / 3600.0, 1);
                totalStudyTime += hours * (p / 100.0);
                totalProgress += p;
            }

            double averageProgress = totalCount > 0 ? Math.Round(totalProgress / totalCount, 1) : 0;

            // Paging for current view
            var enrollments = allEnrollments
                .OrderByDescending(e => e.EnrolledAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var courseIds = enrollments.Select(e => e.CourseId).ToList();

            // Batch fetch stats for courses in the current page
            var courseStats = await _courseRepository.GetEnrollmentsQueryable()
                .AsNoTracking()
                .Where(en => courseIds.Contains(en.CourseId) && en.Status == true)
                .GroupBy(en => en.CourseId)
                .Select(g => new {
                    CourseId = g.Key,
                    TotalStudents = g.Count(),
                    Rating = g.SelectMany(en => en.Comments).Where(cm => cm.Type == CommentType.Review).Any() 
                             ? g.SelectMany(en => en.Comments).Where(cm => cm.Type == CommentType.Review).Average(cm => (double)cm.Rate) 
                             : 0
                })
                .ToDictionaryAsync(x => x.CourseId);

            var courseList = enrollments.Select(e => 
            {
                var totalLessons = e.Course.Lectures.Sum(l => 
                    (l.LectureVideos?.Count ?? 0) + (l.Documents?.Count ?? 0) + (l.Quizzes?.Count ?? 0));

                allProgress.TryGetValue(e.CourseId, out int completedLessons);
                var progress = totalLessons > 0 ? (int)Math.Round((double)completedLessons / totalLessons * 100) : 0;

                courseStats.TryGetValue(e.CourseId, out var stats);

                var totalDuration = e.Course.Lectures.SelectMany(l => l.LectureVideos).Sum(v => v.Duration);
                var totalHours = Math.Round(totalDuration / 3600.0, 1);

                return new MyCourseDTO
                {
                    Id = e.Course.Id,
                    Name = e.Course.Name,
                    ImageUrl = e.Course.ImageUrl,
                    InstructorName = e.Course.Instructor.FullName,
                    EnrolledAt = e.EnrolledAt,
                    Progress = progress,
                    TotalLessons = totalLessons,
                    CompletedLessons = completedLessons,
                    LastVisit = e.LastVisit,
                    Status = e.Course.Status.ToString(),
                    Price = e.Course.Price,
                    TotalHours = totalHours,
                    Rating = stats?.Rating ?? 0,
                    TotalStudents = stats?.TotalStudents ?? 0
                };
            }).ToList();

            var result = new {
                Items = courseList,
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasNextPage = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                CompletedCourses = completedCourses,
                TotalStudyTime = Math.Round(totalStudyTime, 1),
                AverageProgress = (int)Math.Round(averageProgress)
            };

            return new ApiResponse("Success", _localizer["Success"].Value, result, true);
        }

        public async Task<ApiResponse> GetCourseContentAsync(string courseId, string userId)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            var enrollment = await _courseRepository.GetEnrollmentAsync(userId, courseId);
            if (enrollment == null) return new ApiResponse("Forbidden", _localizer["NotEnrolled"].Value, null, false);

            var fullCourse = await _courseRepository.GetQueryable()
                .AsNoTracking()
                .Include(c => c.CourseTags).ThenInclude(ct => ct.Tag)
                .Include(c => c.Lectures.OrderBy(l => l.DisplayOrder))
                    .ThenInclude(l => l.LectureVideos.OrderBy(v => v.DisplayOrder))
                .Include(c => c.Lectures).ThenInclude(l => l.Documents)
                .Include(c => c.Lectures).ThenInclude(l => l.Quizzes)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (fullCourse == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            var progressList = await _courseRepository.GetProgressQueryable()
                .AsNoTracking()
                .Where(p => p.StudentId == userId && p.CourseId == courseId)
                .ToListAsync();

            var completedItemIds = progressList.Where(p => p.IsCompleted == true).Select(p => p.ItemId).ToHashSet();

            var tags = fullCourse.CourseTags != null ? fullCourse.CourseTags.Select(ct => ct.Tag.Name).ToList() : new List<string>();
            var totalSections = fullCourse.Lectures.Count;
            var totalLessons = fullCourse.Lectures.Sum(l => 
                (l.LectureVideos?.Count ?? 0) + (l.Documents?.Count ?? 0) + (l.Quizzes?.Count ?? 0));

            var completedLessons = 0;
            foreach (var l in fullCourse.Lectures)
            {
                completedLessons += (l.LectureVideos?.Count(v => completedItemIds.Contains(v.Id)) ?? 0);
                completedLessons += (l.Documents?.Count(d => completedItemIds.Contains(d.Id)) ?? 0);
                completedLessons += (l.Quizzes?.Count(q => completedItemIds.Contains(q.Id)) ?? 0);
            }

            var progress = totalLessons > 0 ? (int)Math.Round((double)completedLessons / totalLessons * 100) : 0;
            var totalDuration = fullCourse.Lectures.SelectMany(l => l.LectureVideos).Sum(v => v.Duration);
            var totalHours = Math.Round(totalDuration / 3600.0, 1);
            var totalStudyTime = Math.Round(totalHours * (progress / 100.0), 1);

            var response = new CourseContentDTO
            {
                Id = fullCourse.Id,
                Name = fullCourse.Name,
                Tags = tags,
                Progress = progress,
                TotalSections = totalSections,
                TotalHours = totalHours,
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                TotalStudyTime = totalStudyTime,
                UpdatedAt = fullCourse.UpdatedAt,
                Lectures = fullCourse.Lectures.Select(l => 
                {
                    var isLectureCompleted = ((l.LectureVideos?.Count ?? 0) + (l.Documents?.Count ?? 0) + (l.Quizzes?.Count ?? 0)) > 0 
                        && (l.LectureVideos?.All(v => completedItemIds.Contains(v.Id)) ?? true)
                        && (l.Documents?.All(d => completedItemIds.Contains(d.Id)) ?? true)
                        && (l.Quizzes?.All(q => completedItemIds.Contains(q.Id)) ?? true);

                    return new LectureContentDTO
                    {
                        Id = l.Id,
                        Name = l.Name,
                        Description = l.Description ?? string.Empty,
                        DisplayOrder = l.DisplayOrder,
                        IsCompleted = isLectureCompleted,
                        Videos = l.LectureVideos.Select(v => new VideoContentDTO 
                        { 
                            Id = v.Id, 
                            Name = v.Name, 
                            Duration = v.Duration, 
                            DisplayOrder = v.DisplayOrder, 
                            IsCompleted = completedItemIds.Contains(v.Id) 
                        }).ToList(),
                        Documents = l.Documents.Select(d => new DocumentContentDTO 
                        { 
                            Id = d.Id, 
                            Name = d.Name, 
                            Url = d.Url,
                            IsCompleted = completedItemIds.Contains(d.Id) 
                        }).ToList(),
                        Quizzes = l.Quizzes.Select(q => new QuizContentDTO 
                        { 
                            Id = q.Id, 
                            Name = q.Name, 
                            IsCompleted = completedItemIds.Contains(q.Id) 
                        }).ToList()
                    };
                }).ToList()
            };

            return new ApiResponse("Success", _localizer["Success"].Value, response, true);
        }

        #endregion

        #region Instructor Features
        public async Task<ApiResponse> GetInstructorCourseContentAsync(string courseId, string instructorId)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            if (course.InstructorId != instructorId) return new ApiResponse("Forbidden", _localizer["Forbidden"].Value, null, false);

            var fullCourse = await _courseRepository.GetQueryable()
                .AsNoTracking()
                .Include(c => c.CourseTags).ThenInclude(ct => ct.Tag)
                .Include(c => c.Enrollments).ThenInclude(e => e.Comments)
                .Include(c => c.Lectures.OrderBy(l => l.DisplayOrder))
                    .ThenInclude(l => l.LectureVideos.OrderBy(v => v.DisplayOrder))
                .Include(c => c.Lectures).ThenInclude(l => l.Documents)
                .Include(c => c.Lectures).ThenInclude(l => l.Quizzes)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (fullCourse == null) return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            var totalStudents = fullCourse.Enrollments?.Count ?? 0;
            var ratings = fullCourse.Enrollments?.SelectMany(e => e.Comments).Where(cm => cm.Type == CommentType.Review).ToList();
            var rating = ratings != null && ratings.Any() ? ratings.Average(cm => cm.Rate) : 0;

            var tags = fullCourse.CourseTags != null ? fullCourse.CourseTags.Select(ct => ct.Tag.Name).ToList() : new List<string>();
            var totalSections = fullCourse.Lectures.Count;
            var totalLessons = fullCourse.Lectures.Sum(l => 
                (l.LectureVideos?.Count ?? 0) + (l.Documents?.Count ?? 0) + (l.Quizzes?.Count ?? 0));

            var totalDuration = fullCourse.Lectures.SelectMany(l => l.LectureVideos).Sum(v => v.Duration);
            var totalHours = Math.Round(totalDuration / 3600.0, 1);

            var response = new CourseContentDTO
            {
                Id = fullCourse.Id,
                Name = fullCourse.Name,
                Tags = tags,
                Progress = 0,
                TotalSections = totalSections,
                TotalHours = totalHours,
                TotalLessons = totalLessons,
                CompletedLessons = 0,
                TotalStudyTime = 0,
                UpdatedAt = fullCourse.UpdatedAt,
                ImageUrl = fullCourse.ImageUrl,
                Status = fullCourse.Status.ToString(),
                TotalStudents = totalStudents,
                Rating = Math.Round(rating, 1),
                Lectures = fullCourse.Lectures.Select(l => new LectureContentDTO
                {
                    Id = l.Id,
                    Name = l.Name,
                    Description = l.Description ?? string.Empty,
                    DisplayOrder = l.DisplayOrder,
                    IsCompleted = false,
                    Videos = l.LectureVideos.Select(v => new VideoContentDTO 
                    { 
                        Id = v.Id, 
                        Name = v.Name, 
                        Duration = v.Duration, 
                        DisplayOrder = v.DisplayOrder, 
                        IsCompleted = false
                    }).ToList(),
                    Documents = l.Documents.Select(d => new DocumentContentDTO 
                    { 
                        Id = d.Id, 
                        Name = d.Name, 
                        Url = d.Url,
                        IsCompleted = false
                    }).ToList(),
                    Quizzes = l.Quizzes.Select(q => new QuizContentDTO 
                    { 
                        Id = q.Id, 
                        Name = q.Name, 
                        IsCompleted = false
                    }).ToList()
                }).ToList()
            };

            return new ApiResponse("Success", _localizer["Success"].Value, response, true);
        }

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
            _backgroundJobClient.Enqueue(() => _luceneSearchService.IndexCourseAsync(request.Course.Id));
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
