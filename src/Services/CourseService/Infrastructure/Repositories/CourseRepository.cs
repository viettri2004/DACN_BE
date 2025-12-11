using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CourseService.Application.DTOs;
using CourseService.Application.Interfaces;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Shared.Domain.Entities;
using Shared.Infrastructure.cloudinaryService;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace CourseService.Infrastructure.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryService _cloudinaryService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILuceneSearchService _luceneSearchService;
        public CourseRepository(AppDbContext context, CloudinaryService cloudinaryService, IStringLocalizer<SharedResources> localizer, ILuceneSearchService luceneSearchService)
        {
            _context = context;
            _luceneSearchService = luceneSearchService;
            _cloudinaryService = cloudinaryService;
            _localizer = localizer;
        }
        public async Task<ApiResponse> GetCoursesAsync(CourseQueryParameters queryParams, string studentId)
        {
            var query = _context.Courses.AsNoTracking();

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
                    coursesWithPrice = coursesWithPrice.OrderByDescending(x => x.Course.Enrollments.SelectMany(e => e.Comments).Any()
                                                    ? x.Course.Enrollments.SelectMany(e => e.Comments).Average(cm => cm.Rate)
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
                AverageRating = x.Course.Enrollments.SelectMany(e => e.Comments).Any()
                                     ? Math.Round(x.Course.Enrollments.SelectMany(e => e.Comments).Average(cm => cm.Rate), 1)
                                     : 0,
                TotalReviews = x.Course.Enrollments.SelectMany(e => e.Comments).Count(),
                TotalStudents = x.Course.Enrollments.Count,
                OriginalPrice = x.Course.Price,
                Price = x.Price,
                IsBestseller = x.Course.Enrollments.Count > 5,
                TotalHours = 25
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
                    CreateTime = DateTime.UtcNow
                };

                _context.Courses.Add(newCourse);
                await _context.SaveChangesAsync();
                
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
            catch (Exception ex)
            {
                return new ApiResponse("Error", $"Create course failed: {ex.Message}", null, false);
            }
        }

        public async Task<ApiResponse> GetCourseDetailAsync(string courseId, string studentId)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == courseId)
                .Select(c => new CourseDetailDTO
                {
                    Name = c.Name,
                    Description = c.Description,
                    Price = c.Price,
                    ImageUrl = c.ImageUrl,
                    InstructorName = c.Instructor.FullName,
                    Rating = c.Enrollments
                        .SelectMany(e => e.Comments)
                        .Any()
                        ? c.Enrollments.SelectMany(e => e.Comments).Average(cm => cm.Rate)
                        : 0,
                    TotalReviews = c.Enrollments.SelectMany(e => e.Comments).Count(),
                    TotalStudents = c.Enrollments.Count,
                    TotalHours = 36,
                    IsEnrolled = string.IsNullOrEmpty(studentId)
                                ? false
                                : c.Enrollments
                                    .Any(e => e.StudentId == studentId && e.Status == true)
                })
                .FirstOrDefaultAsync();

            if (course == null)
                return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            return new ApiResponse("Success", _localizer["Success"].Value, course, true);
        }

        public async Task<ApiResponse> GetCourseCommentsAsync(string courseId)
        {
            var comments = await _context.Comments
                .AsNoTracking()
                .Where(c => c.Enrollment.CourseId == courseId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDTO
                {
                    CommentId = c.Id,
                    StudentName = c.Enrollment.Student.FullName,
                    Rate = c.Rate,
                    Content = c.Content,
                    Timestamp = c.CreatedAt
                })
                .ToListAsync();

            if (comments.Count.Equals(0))
                return new ApiResponse("Success", _localizer["NoData"].Value, null, false);

            return new ApiResponse("Success", _localizer["Success"].Value, comments, true);
        }

        public async Task<ApiResponse> GetRecommendedCoursesAsync()
        {
            var courseDTOs = await _context.Courses
                .AsNoTracking()
                .OrderByDescending(c => c.CreateTime)
                .Select(c => new CourseListDTO
                {
                    Id = c.Id,
                    ImageUrl = c.ImageUrl,
                    Name = c.Name,
                    InstructorName = c.Instructor.FullName,
                    Rating = c.Enrollments
                        .SelectMany(e => e.Comments)
                        .Any()
                            ? c.Enrollments
                                .SelectMany(e => e.Comments)
                                .Average(cm => cm.Rate)
                            : 0,
                    Price = c.Price
                })
                .Take(5)
                .ToListAsync();

            if (courseDTOs.Count == 0)
                return new ApiResponse("Success", _localizer["NoData"].Value, null, true);

            return new ApiResponse("Success", _localizer["Success"].Value, courseDTOs, true);
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
                    Rating = e.Comments.Any()
                        ? e.Comments.Average(c => c.Rate)
                        : 0,
                    Price = e.Order.OrderItems
                        .Where(oi => oi.CourseId == e.Course.Id)
                        .Sum(oi => (decimal?)oi.Price) ?? 0
                })
                .ToListAsync();

            if (courseDTOs.Count == 0)
                return new ApiResponse("Success", _localizer["NoData"].Value, null, true);

            return new ApiResponse("Success", _localizer["Success"].Value, courseDTOs, true);
        }

        public async Task<ApiResponse> GetCourseContentAsync(string courseId)
        {
            var courseContent = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == courseId)
                .Select(c => new CourseContentDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Lectures = c.Lectures.Select(l => new LectureContentDTO
                    {
                        Id = l.Id,
                        Name = l.Name,
                        Description = l.Description,
                        VideoNames = l.LectureVideos.Select(v => v.Name).ToList(),
                        DocumentNames = l.Documents.Select(d => d.Name).ToList(),
                        QuizNames = l.Quizzes.Select(q => q.Name).ToList()
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (courseContent == null)
            {
                return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            }

            return new ApiResponse("Success", _localizer["Success"].Value, courseContent, true);
        }
    }
}