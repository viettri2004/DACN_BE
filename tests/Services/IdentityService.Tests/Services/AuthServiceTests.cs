using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Application.Services;
using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Application.Interfaces;
using src.Shared.Domain.Entities;
using src.Shared.Resources;
using Xunit;
using IdentityService.Tests.Helpers;

namespace IdentityService.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<IAccountRepository> _mockAccountRepository;
        private readonly Mock<IGoogleAuthService> _mockGoogleAuthService;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var store = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            
            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            _mockRoleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);
            
            _mockTokenService = new Mock<ITokenService>();
            _mockAccountRepository = new Mock<IAccountRepository>();
            _mockGoogleAuthService = new Mock<IGoogleAuthService>();
            _mockNotificationService = new Mock<INotificationService>();
            _localizer = MockHelper.CreateMockLocalizer();

            var googleConfig = Options.Create(new GoogleConfig { ClientId = "test", ClientSecret = "test" });

            _authService = new AuthService(
                _mockAccountRepository.Object,
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockTokenService.Object,
                _localizer,
                _mockGoogleAuthService.Object,
                googleConfig,
                _mockNotificationService.Object);
        }

        [Fact]
        public async Task Register_ShouldReturnConflict_WhenUsernameExists()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                UserName = "existingUser",
                Email = "test@test.com",
                Password = "Password123!",
                FullName = "Test User"
            };

            _mockUserManager.Setup(x => x.FindByNameAsync(registerDto.UserName))
                .ReturnsAsync(new User());

            // Act
            var result = await _authService.Register(registerDto);

            // Assert
            result.Success.Should().BeFalse();
            result.Code.Should().Be("Conflict");
            result.Message.Should().Be("UsernameAlreadyExists");
        }

        [Fact]
        public async Task Register_ShouldReturnConflict_WhenEmailExists()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                UserName = "newUser",
                Email = "existing@test.com",
                Password = "Password123!",
                FullName = "Test User"
            };

            _mockUserManager.Setup(x => x.FindByNameAsync(registerDto.UserName))
                .ReturnsAsync((User)null);
            _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync(new User());

            // Act
            var result = await _authService.Register(registerDto);

            // Assert
            result.Success.Should().BeFalse();
            result.Code.Should().Be("Conflict");
            result.Message.Should().Be("EmailAlreadyExists");
        }

        [Fact]
        public async Task Register_ShouldReturnSuccess_WhenDataIsValid()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                UserName = "newUser",
                Email = "new@test.com",
                Password = "Password123!",
                FullName = "New User"
            };

            _mockUserManager.Setup(x => x.FindByNameAsync(registerDto.UserName))
                .ReturnsAsync((User)null);
            _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((User)null);
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "Student"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.Register(registerDto);

            // Assert
            result.Success.Should().BeTrue();
            result.Code.Should().Be("Created");
            _mockUserManager.Verify(x => x.CreateAsync(It.Is<User>(u => u.Email == registerDto.Email), registerDto.Password), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldReturnSuccess_WhenCredentialsAreCorrect()
        {
            // Arrange
            var loginDto = new LoginDTO
            {
                Username = "testuser",
                Password = "Password123!"
            };

            var user = new User { Id = "1", UserName = loginDto.Username, Email = "test@test.com", IsBanned = false };

            _mockUserManager.Setup(x => x.FindByNameAsync(loginDto.Username))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
                .ReturnsAsync(true);
            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Student" });
            _mockTokenService.Setup(x => x.GenerateAccessTokenAsync(user))
                .ReturnsAsync("test_token");
            _mockTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns("refresh_token");

            // Act
            var (result, refreshToken) = await _authService.LoginAsync(loginDto);

            // Assert
            result.Success.Should().BeTrue();
            result.Code.Should().Be("Success");
            refreshToken.Should().Be("refresh_token");
            result.Data.Should().NotBeNull();
        }
    }
}
