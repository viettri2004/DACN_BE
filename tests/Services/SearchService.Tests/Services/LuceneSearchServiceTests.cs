using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ContentService.Domain.Entities;
using ContentService.Domain.Enums;
using Data.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using SearchService.Application.DTOs;
using SearchService.Application.Services;
using IdentityService.Domain.Entities;
using src.Shared.Resources;
using Xunit;
using SearchService.Tests.Helpers;

namespace SearchService.Tests.Services
{
    public class LuceneSearchServiceTests : IDisposable
    {
        private readonly string _tempPath;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IStringLocalizer<SharedResources>> _mockLocalizer;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly LuceneSearchService _searchService;
        private readonly AppDbContext _context;

        public LuceneSearchServiceTests()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempPath);

            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(m => m.ContentRootPath).Returns(_tempPath);

            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockLocalizer = new Mock<IStringLocalizer<SharedResources>>();
            _mockLocalizer.Setup(x => x[It.IsAny<string>()])
                .Returns((string key) => new LocalizedString(key, key));

            _searchService = new LuceneSearchService(
                _mockScopeFactory.Object,
                _mockLocalizer.Object,
                _mockEnv.Object);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            var mockScope = new Mock<IServiceScope>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(AppDbContext))).Returns(_context);
            mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
            _mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
            // Try to clean up temp path
            try { if (Directory.Exists(_tempPath)) Directory.Delete(_tempPath, true); } catch { }
        }

        [Fact]
        public async Task SearchCourses_ShouldReturnEmpty_WhenNoMatches()
        {
            // Arrange: Index at least one course to initialize Lucene segments
            var course = new Course
            {
                Id = "c1",
                Name = "Initial Course",
                Description = "Desc",
                Status = CourseStatus.Public,
                Price = 100,
                ImageUrl = "img.png",
                InstructorId = "inst1",
                CreateTime = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            await _searchService.IndexCourseAsync(course.Id);

            // Act
            var searchParams = new CourseSearchDTO { SearchTerm = "NonExistent", Page = 1, PageSize = 10 };
            var result = await _searchService.SearchCoursesAsync(searchParams, "user1");

            // Assert
            result.Success.Should().BeTrue();
            var data = result.Data as CourseSearchResponseDTO;
            data.Should().NotBeNull();
            data!.Courses.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task IndexCourse_ShouldStoreCourseInLucene()
        {
            // Arrange
            var courseId = "course1";
            var instructor = new Instructor { Id = "inst1", FullName = "Instructor One", UserName = "inst1", Email = "i@t.com" };
            var course = new Course
            {
                Id = courseId,
                Name = "Test Course",
                Description = "This is a test description",
                Status = CourseStatus.Public,
                Price = 1000,
                ImageUrl = "img.png",
                InstructorId = "inst1",
                CreateTime = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Users.Add(instructor);
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            // Act
            await _searchService.IndexCourseAsync(courseId);

            // Assert
            var searchParams = new CourseSearchDTO { SearchTerm = "Test", Page = 1, PageSize = 10 };
            var searchResult = await _searchService.SearchCoursesAsync(searchParams, "user1");
            searchResult.Success.Should().BeTrue();
            var data = searchResult.Data as CourseSearchResponseDTO;
            data!.Courses.Items.Any(c => c.Id == courseId).Should().BeTrue();
        }
    }
}
