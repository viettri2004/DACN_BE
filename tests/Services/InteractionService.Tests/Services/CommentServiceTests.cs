using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using ContentService.Infrastructure.Repositories;
using Data.Context;
using FluentAssertions;
using Hangfire;
using InteractionService.Application.DTOs;
using InteractionService.Application.Interfaces;
using InteractionService.Application.Services;
using InteractionService.Domain.Entities;
using InteractionService.Domain.Enums;
using InteractionService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using NotificationService.Application.Interfaces;
using SearchService.Application.Interfaces;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using Xunit;
using InteractionService.Tests.Helpers;
using LearningService.Domain.Entities;
using IdentityService.Domain.Entities;
using OrderingService.Domain.Entities;

namespace InteractionService.Tests.Services
{
    public class CommentServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ICommentRepository _commentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly Mock<IStringLocalizer<SharedResources>> _mockLocalizer;
        private readonly Mock<ILuceneSearchService> _mockLuceneSearchService;
        private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
        private readonly Mock<INotificationRepository> _mockNotificationRepository;
        private readonly CommentService _commentService;

        public CommentServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _commentRepository = new CommentRepository(_context);
            _courseRepository = new CourseRepository(_context);

            _mockLocalizer = new Mock<IStringLocalizer<SharedResources>>();
            _mockLocalizer.Setup(x => x[It.IsAny<string>()])
                .Returns((string key) => new LocalizedString(key, key));

            _mockLuceneSearchService = new Mock<ILuceneSearchService>();
            _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
            _mockNotificationRepository = new Mock<INotificationRepository>();

            _commentService = new CommentService(
                _commentRepository,
                _courseRepository,
                _mockLocalizer.Object,
                _mockLuceneSearchService.Object,
                _mockBackgroundJobClient.Object,
                _context,
                _mockNotificationRepository.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task AddComment_ShouldReturnForbidden_WhenNotEnrolled()
        {
            // Arrange
            var dto = new AddCommentDTO { CourseId = "course1", Content = "Test", Type = CommentType.Review, Rate = 5 };
            var userId = "user1";

            // Act
            var result = await _commentService.AddCommentAsync(dto, userId);

            // Assert
            result.Success.Should().BeFalse();
            result.Code.Should().Be("Forbidden");
            result.Message.Should().Be("NotEnrolledInCourse");
        }

        [Fact]
        public async Task AddComment_ShouldReturnCreated_WhenEnrolled()
        {
            // Arrange
            var userId = "user1";
            var courseId = "course1";
            var dto = new AddCommentDTO { CourseId = courseId, Content = "Test", Type = CommentType.Review, Rate = 5 };

            // Seed enrollment and related data
            var student = new Student { Id = userId, UserName = "std1", Email = "s@t.com", FullName = "Student One" };
            var instructor = new Instructor { Id = "inst1", UserName = "inst1", Email = "i@t.com", FullName = "Instructor One" };
            var course = new Course { Id = courseId, Name = "Course 1", InstructorId = "inst1", Description = "Desc", ImageUrl = "img.png" };
            var order = new Order { Id = "order1", StudentId = userId, TotalAmount = 100, Status = "Completed", CreatedAt = DateTime.UtcNow };
            
            _context.Users.AddRange(student, instructor);
            _context.Courses.Add(course);
            _context.Orders.Add(order);
            _context.Enrollments.Add(new Enrollment 
            { 
                Id = "enr1", 
                StudentId = userId, 
                CourseId = courseId, 
                OrderId = "order1",
                Status = true,
                EnrolledAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _commentService.AddCommentAsync(dto, userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Code.Should().Be("Created");
            _context.Comments.Any(c => c.UserId == userId && c.Content == dto.Content).Should().BeTrue();
        }

        [Fact]
        public async Task GetCourseComments_ShouldCalculateAverageRatingCorrectly()
        {
            // Arrange
            var courseId = "course1";
            var user1 = new Student { Id = "u1", FullName = "User 1", UserName = "u1", Email = "u1@t.com" };
            var user2 = new Student { Id = "u2", FullName = "User 2", UserName = "u2", Email = "u2@t.com" };
            _context.Users.AddRange(user1, user2);

            _context.Comments.AddRange(
                new Comment { Id = "c1", CourseId = courseId, UserId = "u1", Content = "Good", Rate = 5, Type = CommentType.Review, CreatedAt = DateTime.UtcNow },
                new Comment { Id = "c2", CourseId = courseId, UserId = "u2", Content = "Avg", Rate = 3, Type = CommentType.Review, CreatedAt = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _commentService.GetCourseCommentsAsync(courseId, null, CommentType.Review, 1, 10);

            // Assert
            result.Success.Should().BeTrue();
            var data = result.Data as PagedCommentResultDTO;
            data.Should().NotBeNull();
            data!.AverageRating.Should().Be(4.0);
            data.TotalRatingCount.Should().Be(2);
            data.Items.Count.Should().Be(2);
        }
    }
}
