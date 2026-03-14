using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
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

namespace CourseService.Infrastructure.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly AppDbContext _context;
        private readonly CloudinaryService _cloudinaryService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILuceneSearchService _luceneSearchService;
        private readonly INotificationRepository _notificationRepository;

        public CourseRepository(AppDbContext context, 
                               CloudinaryService cloudinaryService, 
                               IStringLocalizer<SharedResources> localizer, 
                               ILuceneSearchService luceneSearchService,
                               INotificationRepository notificationRepository)
        {
            _context = context;
            _luceneSearchService = luceneSearchService;
            _cloudinaryService = cloudinaryService;
            _localizer = localizer;
            _notificationRepository = notificationRepository;
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
                    TotalHours = 0,
                    IsEnrolled = string.IsNullOrEmpty(studentId)
                                ? false
                                : c.Enrollments
                                    .Any(e => e.StudentId == studentId && e.Status == true),
                    // Status = c.Status.ToString()
                })
                .FirstOrDefaultAsync();

            if (course == null)
                return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            return new ApiResponse("Success", _localizer["Success"].Value, course, true);
        }

        public async Task<ApiResponse> GetCourseCommentsAsync(string courseId, string? userId)
        {
            var allComments = await _context.Comments
                .AsNoTracking()
                .Include(c => c.Enrollment.Student)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.Enrollment.Student)
                .Where(c => c.Enrollment.CourseId == courseId && c.ReplyId == null)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDTO
                {
                    CommentId = c.Id,
                    UserName = c.Enrollment.Student.FullName,
                    AvatarUrl = c.Enrollment.Student.AvatarUrl,
                    Rate = c.Rate,
                    Content = c.Content,
                    IsMyComment = userId != null && c.Enrollment.StudentId == userId,
                    Timestamp = c.CreatedAt,
                    Replies = c.Replies.Select(r => new ReplyDTO
                    {
                        CommentId = r.Id,
                        Content = r.Content,
                        Timestamp = r.CreatedAt
                    }).OrderBy(r => r.Timestamp).ToList()
                })
                .ToListAsync();

            var response = new CourseCommentsResponseDTO
            {
                MyComment = allComments.FirstOrDefault(c => c.IsMyComment),
                AllComments = allComments.Where(c => !c.IsMyComment).ToList()
            };

            return new ApiResponse("Success", _localizer["Success"].Value, response, true);
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
                    Price = c.Price,
                    // Status = c.Status.ToString()
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
                        .Sum(oi => (decimal?)oi.Price) ?? 0,
                    // Status = e.Course.Status.ToString()
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
                        .Any()
                            ? c.Enrollments
                                .SelectMany(e => e.Comments)
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
            var notification = new Notification
            {
                Title = "New Course Approval Request",
                Message = $"Instructor {course.Instructor.FullName} has submitted a new course: {course.Name}",
                Type = NotificationType.CourseRequest,
                Role = NotificationRole.Admin,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepository.CreateNotificationAsync(notification);

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
                // Update Index
                try 
                {
                    await _luceneSearchService.IndexCourseAsync(request.Course);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Indexing failed: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

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
                AverageRating = c.Enrollments.Any(e => e.Comments.Any()) 
                                ? c.Enrollments.SelectMany(e => e.Comments).Average(cm => cm.Rate) 
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
                .FirstOrDefaultAsync(c => c.EnrollmentId == enrollment.Id);

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
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return new ApiResponse("Created", _localizer["CommentAdded"].Value, null, true);
        }

        public async Task<ApiResponse> UpdateCommentAsync(string commentId, UpdateCommentDTO updateCommentDTO, string userId)
        {
            var comment = await _context.Comments
                .Include(c => c.Enrollment)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return new ApiResponse("NotFound", _localizer["CommentNotFound"].Value, null, false);
            }

            if (comment.Enrollment.StudentId != userId)
            {
                return new ApiResponse("Forbidden", _localizer["Unauthorized"].Value, null, false);
            }

            comment.Content = updateCommentDTO.Content;
            comment.Rate = updateCommentDTO.Rate;
            comment.CreatedAt = DateTime.UtcNow; 

            await _context.SaveChangesAsync();

            return new ApiResponse("Success", _localizer["CommentUpdated"].Value, null, true);
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
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(reply);
            await _context.SaveChangesAsync();

            return new ApiResponse("Created", _localizer["ReplyAdded"].Value, null, true);
        }
    }
}
