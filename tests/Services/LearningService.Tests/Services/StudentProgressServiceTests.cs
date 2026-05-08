using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using ContentService.Infrastructure.Repositories;
using Data.Context;
using FluentAssertions;
using LearningService.Application.DTOs;
using LearningService.Application.Services;
using LearningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using ContentService.Application.DTOs;
using IdentityService.Domain.Entities;
using OrderingService.Domain.Entities;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using Xunit;
using LearningService.Tests.Helpers;

namespace LearningService.Tests.Services
{
    public class StudentProgressServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ICourseRepository _courseRepository;
        private readonly Mock<IStringLocalizer<SharedResources>> _mockLocalizer;
        private readonly StudentProgressService _studentProgressService;

        public StudentProgressServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _courseRepository = new CourseRepository(_context);

            _mockLocalizer = new Mock<IStringLocalizer<SharedResources>>();
            _mockLocalizer.Setup(x => x[It.IsAny<string>()])
                .Returns((string key) => new LocalizedString(key, key));

            _studentProgressService = new StudentProgressService(_courseRepository, _mockLocalizer.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task MarkItemCompleted_ShouldCreateNewProgress_WhenNotExists()
        {
            // Arrange
            var dto = new MarkItemCompletedDTO
            {
                CourseId = "course1",
                LectureId = "lecture1",
                ItemId = "video1",
                ItemType = "Video"
            };
            var studentId = "student1";

            // Act
            var result = await _studentProgressService.MarkItemCompletedAsync(dto, studentId);

            // Assert
            result.Success.Should().BeTrue();
            var progress = await _context.StudentLectureProgresses.FirstOrDefaultAsync(p => p.StudentId == studentId && p.ItemId == dto.ItemId);
            progress.Should().NotBeNull();
            progress!.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task GetContinueLearningCourses_ShouldReturnCorrectProgress()
        {
            // Arrange
            var studentId = "student1";
            var instructor = new Instructor { Id = "inst1", FullName = "Instructor One", UserName = "inst1", Email = "i@t.com" };
            var course = new Course { Id = "course1", Name = "Course 1", InstructorId = "inst1", Description = "Desc", ImageUrl = "img.png" };
            var lecture = new Lecture { Id = "lec1", CourseId = "course1", Name = "Lecture 1" };
            var video1 = new LectureVideo { Id = "vid1", LectureId = "lec1", Name = "Video 1", VideoUrl = "v1.mp4" };
            var video2 = new LectureVideo { Id = "vid2", LectureId = "lec1", Name = "Video 2", VideoUrl = "v2.mp4" };
            
            _context.Users.Add(instructor);
            _context.Courses.Add(course);
            _context.Lectures.Add(lecture);
            _context.LectureVideos.AddRange(video1, video2);

            var order = new Order { Id = "order1", StudentId = studentId, TotalAmount = 100000, Status = "Completed", CreatedAt = DateTime.UtcNow };
            _context.Orders.Add(order);
            
            _context.Enrollments.Add(new Enrollment 
            { 
                Id = "enr1", 
                StudentId = studentId, 
                CourseId = "course1", 
                OrderId = "order1",
                Status = true, 
                LastVisit = DateTime.UtcNow,
                EnrolledAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            });

            // Mark one video as completed
            _context.StudentLectureProgresses.Add(new StudentLectureProgress 
            { 
                Id = "prog1", 
                StudentId = studentId, 
                CourseId = "course1", 
                LectureId = "lec1", 
                ItemId = "vid1", 
                ItemType = "Video", 
                IsCompleted = true 
            });

            await _context.SaveChangesAsync();

            // Act
            var result = await _studentProgressService.GetContinueLearningCoursesAsync(studentId);

            // Assert
            result.Success.Should().BeTrue();
            var courses = result.Data as List<MyCourseDTO>;
            courses.Should().NotBeNull();
            courses!.Count.Should().Be(1);
            courses[0].Progress.Should().Be(50); // 1/2 completed
            courses[0].TotalLessons.Should().Be(2);
            courses[0].CompletedLessons.Should().Be(1);
        }
    }
}
