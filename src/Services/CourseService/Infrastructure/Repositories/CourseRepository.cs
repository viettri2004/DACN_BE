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
        public CourseRepository(AppDbContext context, CloudinaryService cloudinaryService, IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _localizer = localizer;
        }
        public async Task<ApiResponse> GetCoursesAsync(CourseQueryParameters queryParams)
        {
            var query = _context.Courses.AsNoTracking();

            
            if (!string.IsNullOrEmpty(queryParams.TagId))
            {
                query = query.Where(c => c.CourseTags.Any(ct => ct.TagId == queryParams.TagId));
            }

            if (queryParams.MinPrice.HasValue)
            {
                query = query.Where(c =>
                    (int.Parse(c.Id.Substring(0, 1), System.Globalization.NumberStyles.HexNumber) % 2 != 0 ? c.Price * 0.5m : c.Price) >= queryParams.MinPrice.Value
                );
            }
            if (queryParams.MaxPrice.HasValue)
            {
                query = query.Where(c =>
                    (int.Parse(c.Id.Substring(0, 1), System.Globalization.NumberStyles.HexNumber) % 2 != 0 ? c.Price * 0.5m : c.Price) <= queryParams.MaxPrice.Value
                );
            }

            var totalCount = await query.CountAsync();

            switch (queryParams.SortBy?.ToLower())
            {
                case "rating": 
                    query = query.OrderByDescending(c => c.Enrollments.SelectMany(e => e.Comments).Any()
                                                          ? c.Enrollments.SelectMany(e => e.Comments).Average(cm => cm.Rate)
                                                          : 0);
                    break;
                case "newest": 
                    query = query.OrderByDescending(c => c.CreateTime);
                    break;
                case "priceasc": 
                    query = query.OrderBy(c => (int.Parse(c.Id.Substring(0, 1), System.Globalization.NumberStyles.HexNumber) % 2 != 0 ? c.Price * 0.5m : c.Price));
                    break;
                case "pricedesc": 
                    query = query.OrderByDescending(c => (int.Parse(c.Id.Substring(0, 1), System.Globalization.NumberStyles.HexNumber) % 2 != 0 ? c.Price * 0.5m : c.Price));
                    break;
                case "popularity": 
                default:
                    query = query.OrderByDescending(c => c.Enrollments.Count);
                    break;
            }

            var pagedCoursesData = await query
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .Select(c => new CourseCardDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    ImageUrl = c.ImageUrl,
                    InstructorName = c.Instructor.FullName,
                    AverageRating = c.Enrollments.SelectMany(e => e.Comments).Any()
                        ? Math.Round(c.Enrollments.SelectMany(e => e.Comments).Average(cm => cm.Rate), 1)
                        : 0,
                    TotalReviews = c.Enrollments.SelectMany(e => e.Comments).Count(),
                    TotalStudents = c.Enrollments.Count,
                    OriginalPrice = c.Price, 
                    Price = (int.Parse(c.Id.Substring(0, 1), System.Globalization.NumberStyles.HexNumber) % 2 != 0)
                            ? (c.Price * 0.5m) 
                            : c.Price,         
                    IsBestseller = c.Enrollments.Count > 5, 
                    TotalHours = 25 
                })
                .ToListAsync();

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
                if (createCourseDTO.image != null)
                {
                    imageUrl = await _cloudinaryService.UploadImageAsync(createCourseDTO.image);
                }

                var newCourse = new Course
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = createCourseDTO.name,
                    Price = createCourseDTO.price,
                    Description = createCourseDTO.description ?? string.Empty,
                    ImageUrl = imageUrl,
                    InstructorId = instructorId,
                    CreateTime = DateTime.UtcNow
                };

                _context.Courses.Add(newCourse);
                await _context.SaveChangesAsync();

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

        public async Task<ApiResponse> GetCourseDetailAsync(string courseId)
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
                    TotalHours = 36
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
    }
}