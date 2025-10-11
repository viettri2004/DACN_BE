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
        private readonly IMapper _mapper;
        public CourseRepository(AppDbContext context, CloudinaryService cloudinaryService, IStringLocalizer<SharedResources> localizer, IMapper mapper)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _localizer = localizer;
            _mapper = mapper;
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
                    Description = createCourseDTO.description,
                    ImageUrl = imageUrl,
                    InstructorId = instructorId,
                    CreateTime = DateTime.UtcNow
                };

                _context.Courses.Add(newCourse);
                await _context.SaveChangesAsync();

                return new ApiResponse(
                    "Success",
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
            var course = await _context.Set<Course>()
                .Include(c => c.Instructor)
                .Include(c => c.LeaveComments)
                .Include(c => c.Lectures)
                .Include(c => c.StudentCourses)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);

            var dto = _mapper.Map<CourseDetailDTO>(course);

            dto.Rating = course.LeaveComments.Any() ? course.LeaveComments.Average(lc => lc.Rate) : 0;

            dto.TotalStudents = course.StudentCourses.Count;

            dto.TotalReviews = course.LeaveComments.Count;

            dto.TotalHours = 36;

            return new ApiResponse("Success", "", dto, true);
        }
        public async Task<ApiResponse> GetCourseCommentsAsync(string courseId)
        {
            var comments = await _context.Set<LeaveComment>()
                .Where(c => c.CourseId == courseId)
                .OrderByDescending(c => c.Timestamp)
                .Include(c => c.Student) 
                .ToListAsync();

            var commentDTOs = _mapper.Map<List<LeaveCommentDTO>>(comments);

            if (comments == null || !comments.Any())
                return new ApiResponse("Success", _localizer["NoData"].Value, null, false);

            return new ApiResponse("Success", "", commentDTOs, true);
        }

    }
}