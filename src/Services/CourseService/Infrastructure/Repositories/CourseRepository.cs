using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public CourseRepository(AppDbContext context, CloudinaryService cloudinaryService, IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _localizer = localizer;
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
    }
}