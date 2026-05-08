using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentService.Application.DTOs;
using ContentService.Application.Interfaces;
using ContentService.Application.Services;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using ContentService.Infrastructure.Repositories;
using Data.Context;
using FluentAssertions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Moq;
using NotificationService.Application.Interfaces;
using SearchService.Application.Interfaces;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using Xunit;
using ContentService.Tests.Helpers;
using IdentityService.Domain.Entities;

namespace ContentService.Tests.Services
{
    public class CourseServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ICourseRepository _courseRepository;
        private readonly Mock<IStringLocalizer<SharedResources>> _mockLocalizer;
        private readonly Mock<ILuceneSearchService> _mockLuceneSearchService;
        private readonly Mock<INotificationRepository> _mockNotificationRepository;
        private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly CourseService _courseService;

        public CourseServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _courseRepository = new CourseRepository(_context);

            _mockLocalizer = new Mock<IStringLocalizer<SharedResources>>();
            _mockLuceneSearchService = new Mock<ILuceneSearchService>();
            _mockNotificationRepository = new Mock<INotificationRepository>();
            _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
            _mockCache = new Mock<IDistributedCache>();
            _mockNotificationService = new Mock<INotificationService>();

            // Setup localizer
            _mockLocalizer.Setup(x => x[It.IsAny<string>()])
                .Returns((string key) => new LocalizedString(key, key));

            _courseService = new CourseService(
                _courseRepository,
                _mockLocalizer.Object,
                _mockLuceneSearchService.Object,
                _mockNotificationRepository.Object,
                _mockBackgroundJobClient.Object,
                _mockCache.Object,
                _mockNotificationService.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateCourse_ShouldReturnCreated_WhenDataIsValid()
        {
            // Arrange
            var createDto = new CreateCourseDTO
            {
                name = "New Course",
                price = 100000,
                description = "Description",
                imageUrl = "image.png",
                TagIds = new List<string> { "tag1" }
            };
            var instructorId = "instructor1";

            _context.Tags.Add(new Tag { Id = "tag1", Name = "Tag 1", Description = "Tag Description" });
            await _context.SaveChangesAsync();

            // Act
            var result = await _courseService.CreateCourseAsync(createDto, instructorId);

            // Assert
            result.Success.Should().BeTrue();
            result.Code.Should().Be("Created");
            
            var courseInDb = await _context.Courses.FirstOrDefaultAsync(c => c.Name == createDto.name);
            courseInDb.Should().NotBeNull();
            courseInDb!.InstructorId.Should().Be(instructorId);
        }

        [Fact]
        public async Task GetCourseDetail_ShouldReturnNotFound_WhenCourseDoesNotExist()
        {
            // Arrange
            var courseId = "nonexistent";

            // Act
            var result = await _courseService.GetCourseDetailAsync(courseId, "student1");

            // Assert
            result.Success.Should().BeFalse();
            result.Code.Should().Be("NotFound");
            result.Message.Should().Be("CourseNotFound");
        }

        [Fact]
        public async Task UpdateCourse_ShouldReturnSuccess_WhenDataIsValid()
        {
            // Arrange
            var courseId = "course1";
            var instructorId = "instructor1";
            
            var instructor = new Instructor { Id = instructorId, UserName = "instr", Email = "i@t.com", FullName = "Instr" };
            _context.Users.Add(instructor);

            var existingCourse = new Course 
            { 
                Id = courseId, 
                Name = "Old Name", 
                InstructorId = instructorId,
                Description = "Old Description",
                ImageUrl = "old.png"
            };
            
            _context.Courses.Add(existingCourse);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateCourseDTO
            {
                name = "New Name",
                price = 200000,
                description = "New Description"
            };

            // Act
            var result = await _courseService.UpdateCourseAsync(courseId, updateDto, instructorId);

            // Assert
            result.Success.Should().BeTrue(result.Message);
            result.Code.Should().Be("Success");
            
            _context.Entry(existingCourse).Reload();
            existingCourse.Name.Should().Be(updateDto.name);
        }
    }
}
