using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using NotificationService.Application.DTOs;
using NotificationService.Application.Services;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using Shared.Infrastructure.Hubs;
using Shared.Domain.Entities;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using Xunit;
using NotificationService.Tests.Helpers;
using IdentityService.Domain.Entities;

namespace NotificationService.Tests.Services
{
    public class NotificationServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IStringLocalizer<SharedResources>> _mockLocalizer;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Application.Services.NotificationService _notificationService;

        public NotificationServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _mockLocalizer = new Mock<IStringLocalizer<SharedResources>>();
            _mockLocalizer.Setup(x => x[It.IsAny<string>()])
                .Returns((string key) => new LocalizedString(key, key));

            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(x => x.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClients.Setup(x => x.All).Returns(_mockClientProxy.Object);

            _notificationService = new Application.Services.NotificationService(
                _context,
                _mockLocalizer.Object,
                _mockHubContext.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateNotification_ShouldSaveToDatabaseAndSendToSignalR()
        {
            // Arrange
            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "user1",
                Title = "Test Title",
                Message = "Test Message",
                Type = NotificationType.System,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _notificationService.CreateNotificationAsync(notification);

            // Assert
            result.Success.Should().BeTrue();
            result.Code.Should().Be("Created");
            _context.Notifications.Any(n => n.Id == notification.Id).Should().BeTrue();
            _mockClients.Verify(x => x.Group(notification.UserId), Times.Once);
            _mockClientProxy.Verify(x => x.SendCoreAsync("ReceiveNotification", It.IsAny<object[]>(), default), Times.Once);
        }

        [Fact]
        public async Task GetUserNotifications_ShouldReturnPaginatedResults()
        {
            // Arrange
            var userId = "user1";
            _context.Users.Add(new Student { Id = userId, UserName = "u1", Email = "u1@t.com", FullName = "U1" });
            
            for (int i = 0; i < 15; i++)
            {
                _context.Notifications.Add(new Notification
                {
                    Id = $"n{i}",
                    UserId = userId,
                    Title = $"Title {i}",
                    Message = "Msg",
                    Type = NotificationType.System,
                    CreatedAt = DateTime.UtcNow.AddMinutes(i)
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var result = await _notificationService.GetUserNotificationsAsync(userId, page: 1, pageSize: 10);

            // Assert
            result.Success.Should().BeTrue();
            var data = result.Data as PagedResult<NotificationDTO>;
            data.Should().NotBeNull();
            data!.Items.Count.Should().Be(10);
            data.TotalCount.Should().Be(15);
        }

        [Fact]
        public async Task MarkAsRead_ShouldUpdateStatus()
        {
            // Arrange
            var notificationId = "n1";
            var notification = new Notification
            {
                Id = notificationId,
                UserId = "u1",
                Title = "T",
                Message = "M",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Act
            var result = await _notificationService.MarkAsReadAsync(notificationId);

            // Assert
            result.Success.Should().BeTrue();
            var updated = await _context.Notifications.FindAsync(notificationId);
            updated!.IsRead.Should().BeTrue();
        }
    }
}
