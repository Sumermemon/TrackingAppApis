using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RunningCompetition.Application.DTOs.Auth;
using RunningCompetition.Application.Services;
using RunningCompetition.Contracts.Repositories;
using RunningCompetition.Contracts.Services;
using RunningCompetition.Domain.Entities;
using RunningCompetition.Domain.Enums;
using RunningCompetition.Shared.Exceptions;
using RunningCompetition.Shared.Settings;
using Xunit;

namespace RunningCompetition.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly Mock<IReferralRepository> _referralRepoMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly IOptions<JwtSettings> _jwtSettingsOptions;
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();

    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var jwtSettings = new JwtSettings
        {
            Secret = "SuperSecretKeyForTestingNeedToBeAtLeast32Bytes",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        };
        _jwtSettingsOptions = Options.Create(jwtSettings);

        _authService = new AuthService(
            _userRepoMock.Object,
            _refreshTokenRepoMock.Object,
            _referralRepoMock.Object,
            _tokenServiceMock.Object,
            _emailServiceMock.Object,
            _cacheServiceMock.Object,
            _mapperMock.Object,
            _jwtSettingsOptions,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowConflictException_WhenEmailExists()
    {
        // Arrange
        var request = new RegisterRequest("Test", "User", "test@example.com", "Password123!", null);
        _userRepoMock.Setup(repo => repo.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = request.Email });

        // Act
        Func<Task> act = async () => await _authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*email already exists*");
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndReturnTokens_WhenValid()
    {
        // Arrange
        var request = new RegisterRequest("John", "Doe", "john@example.com", "Password123!", null);
        _userRepoMock.Setup(repo => repo.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
            
        _tokenServiceMock.Setup(ts => ts.GenerateAccessTokenAsync(It.IsAny<Guid>(), request.Email, It.IsAny<List<string>>(), It.IsAny<List<string>>()))
            .ReturnsAsync("access_token");
        _tokenServiceMock.Setup(ts => ts.GenerateRefreshToken())
            .Returns("refresh_token");

        _mapperMock.Setup(m => m.Map<UserSummaryDto>(It.IsAny<User>()))
            .Returns(new UserSummaryDto(Guid.NewGuid(), "John", "Doe", request.Email, null, [], false, false));

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");

        _userRepoMock.Verify(repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(es => es.SendEmailVerificationAsync(request.Email, "John Doe", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenUserNotFound()
    {
        // Arrange
        var request = new LoginRequest("unknown@example.com", "Password", null);
        _userRepoMock.Setup(repo => repo.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _authService.LoginAsync(request, "127.0.0.1");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var password = "ValidPassword123!";
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "john@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Status = UserStatus.Active
        };
        var request = new LoginRequest(user.Email, password, null);

        _userRepoMock.Setup(repo => repo.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock.Setup(repo => repo.GetWithRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tokenServiceMock.Setup(ts => ts.GenerateAccessTokenAsync(user.Id, user.Email, It.IsAny<List<string>>(), It.IsAny<List<string>>()))
            .ReturnsAsync("new_access_token");
        _tokenServiceMock.Setup(ts => ts.GenerateRefreshToken())
            .Returns("new_refresh_token");

        _mapperMock.Setup(m => m.Map<UserSummaryDto>(It.IsAny<User>()))
            .Returns(new UserSummaryDto(user.Id, "John", "Doe", user.Email, null, [], true, false));

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");

        _userRepoMock.Verify(repo => repo.Update(It.IsAny<User>()), Times.Once);
        _refreshTokenRepoMock.Verify(repo => repo.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
