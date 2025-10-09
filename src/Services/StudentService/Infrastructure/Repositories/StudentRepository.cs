using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CourseService.Application.DTOs;
using Data.Context;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using StudentService.Application.Interfaces;

namespace StudentService.Infrastructure.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<SharedResources> _localizer;
        public StudentRepository(AppDbContext context, IMapper mapper, IStringLocalizer<SharedResources> localizer)
        {
            _localizer = localizer;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse> GetRecommendedCoursesAsync()
        {
            var courses = await _context.Set<Course>()
                .Include(c => c.Instructor)
                .Include(c => c.LeaveComments)
                .OrderByDescending(c => c.CreateTime)
                .Take(5)
                .ToListAsync();

            if (courses == null || !courses.Any())
            {
                return new ApiResponse("NotFound", _localizer["NotFound"].Value, null, false);
            }

            var courseDTOs = courses.Select(c =>
            {
                var dto = _mapper.Map<CourseListDTO>(c);
                dto.Rating = c.LeaveComments.Any()
                    ? c.LeaveComments.Average(lc => lc.Rate)
                    : 0;
                return dto;
            }).ToList();

            return new ApiResponse("Success", "", courseDTOs, true);
        }
        public async Task<ApiResponse> GetMyCoursesAsync(string studentId)
        {
            var studentCourses = await _context.Set<StudentCourse>()
                .Include(sc => sc.Course)
                    .ThenInclude(c => c.Instructor)
                .Include(sc => sc.Course)
                    .ThenInclude(c => c.LeaveComments)
                .Where(sc => sc.StudentId == studentId)
                .ToListAsync();

            if (studentCourses == null || !studentCourses.Any())
                return new ApiResponse("NotFound", "NotFound", null, false);

            var courseDTOs = studentCourses.Select(sc =>
            {
                var dto = _mapper.Map<CourseListDTO>(sc.Course);
                dto.Rating = sc.Course.LeaveComments.Any() ? sc.Course.LeaveComments.Average(lc => lc.Rate) : 0;
                dto.Price = sc.Amount; 
                return dto;
            }).ToList();

            return new ApiResponse("Success", "", courseDTOs, true);
        }

    }
}