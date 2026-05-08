using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moq;
using OrderingService.Application.DTOs;
using OrderingService.Application.Interfaces;
using OrderingService.Application.Services;
using OrderingService.Domain.Entities;
using Data.Context;
using SearchService.Application.Interfaces;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using Hangfire;
using src.Shared.Resources;
using Xunit;
using OrderingService.Tests.Helpers;
using LearningService.Domain.Entities;
using ContentService.Domain.Entities;
using IdentityService.Domain.Entities;
using src.Shared.Domain.Entities;

namespace OrderingService.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<IPaymentRepository> _mockPaymentRepository;
        private readonly Mock<IVnPayService> _mockVnPayService;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
        private readonly Mock<ILuceneSearchService> _mockLuceneSearchService;
        private readonly Mock<INotificationRepository> _mockNotificationRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly AppDbContext _context;
        private readonly PaymentService _paymentService;

        public PaymentServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _mockPaymentRepository = new Mock<IPaymentRepository>();
            _mockVnPayService = new Mock<IVnPayService>();
            _mockCache = new Mock<IDistributedCache>();
            _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
            _mockLuceneSearchService = new Mock<ILuceneSearchService>();
            _mockNotificationRepository = new Mock<INotificationRepository>();
            _localizer = MockHelper.CreateMockLocalizer();

            var mockConfig = new Mock<IConfiguration>();
            var mockSection = new Mock<IConfigurationSection>();
            mockConfig.Setup(x => x.GetSection("Bank")).Returns(mockSection.Object);

            _paymentService = new PaymentService(
                _mockPaymentRepository.Object,
                _mockVnPayService.Object,
                mockConfig.Object,
                _mockCache.Object,
                _localizer,
                _mockBackgroundJobClient.Object,
                _mockLuceneSearchService.Object,
                _context,
                _mockNotificationRepository.Object);
        }

        [Fact]
        public async Task RedeemGiftCode_ShouldReturnNotFound_WhenCodeIsInvalid()
        {
            // Arrange
            var redeemDto = new GiftCodeRedeemDto { Code = "INVALID", CourseId = "course1" };
            _mockPaymentRepository.Setup(x => x.GetGiftCodeByCodeAndCourseAsync(redeemDto.Code, redeemDto.CourseId))
                .ReturnsAsync((GiftCode)null);
            _mockPaymentRepository.Setup(x => x.GetGiftCodeByCodeAndCourseAsync(redeemDto.Code, null))
                .ReturnsAsync((GiftCode)null);

            // Act
            var result = await _paymentService.RedeemGiftCodeAsync(redeemDto, "student1");

            // Assert
            result.Success.Should().BeFalse();
            result.Code.Should().Be("NotFound");
            result.Message.Should().Be("InvalidGiftCode");
        }

        [Fact]
        public async Task RedeemGiftCode_ShouldReturnBadRequest_WhenUsageLimitReached()
        {
            // Arrange
            var redeemDto = new GiftCodeRedeemDto { Code = "LIMIT", CourseId = "course1" };
            var giftCode = new GiftCode 
            { 
                Code = "LIMIT", 
                IsActive = true, 
                MaxUses = 1, 
                UsageCount = 1,
                CourseId = "course1"
            };

            _mockPaymentRepository.Setup(x => x.GetGiftCodeByCodeAndCourseAsync(redeemDto.Code, redeemDto.CourseId))
                .ReturnsAsync(giftCode);

            // Act
            var result = await _paymentService.RedeemGiftCodeAsync(redeemDto, "student1");

            // Assert
            result.Success.Should().BeFalse();
            result.Code.Should().Be("BadRequest");
            result.Message.Should().Be("GiftCodeUsageLimitReached");
        }

        [Fact]
        public async Task RedeemGiftCode_ShouldReturnSuccess_WhenDataIsValid()
        {
            // Arrange
            var redeemDto = new GiftCodeRedeemDto { Code = "VALID", CourseId = "course1" };
            var giftCode = new GiftCode 
            { 
                Code = "VALID", 
                IsActive = true, 
                MaxUses = 10, 
                UsageCount = 0,
                CourseId = "course1"
            };
            var course = new Course { Id = "course1", Name = "Test Course", Price = 100, InstructorId = "instructor1" };
            var student = new User { Id = "student1", FullName = "Student One" };

            _mockPaymentRepository.Setup(x => x.GetGiftCodeByCodeAndCourseAsync(redeemDto.Code, redeemDto.CourseId))
                .ReturnsAsync(giftCode);
            _mockPaymentRepository.Setup(x => x.GetCourseByIdAsync(redeemDto.CourseId))
                .ReturnsAsync(course);
            _mockPaymentRepository.Setup(x => x.GetEnrollmentsByStudentAndCoursesAsync("student1", It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Enrollment>());
            _mockPaymentRepository.Setup(x => x.GetUserByIdAsync("student1"))
                .ReturnsAsync(student);

            // Act
            var result = await _paymentService.RedeemGiftCodeAsync(redeemDto, "student1");

            // Assert
            result.Success.Should().BeTrue();
            result.Code.Should().Be("Success");
            _mockPaymentRepository.Verify(x => x.AddEnrollmentAsync(It.IsAny<Enrollment>()), Times.Once);
            _mockPaymentRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
            giftCode.UsageCount.Should().Be(1);
        }
    }
}
