using System.Security.Cryptography;
using AutoMapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunningCompetition.Application.DTOs.Auth;
using RunningCompetition.Contracts.Repositories;
using RunningCompetition.Contracts.Services;
using RunningCompetition.Domain.Entities;
using RunningCompetition.Domain.Enums;
using RunningCompetition.Shared.Constants;
using RunningCompetition.Shared.Exceptions;
using RunningCompetition.Shared.Settings;

namespace RunningCompetition.Application.Services;

/// <summary>Handles all authentication business logic.</summary>
public sealed class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IReferralRepository _referralRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    /// <summary>Initializes a new instance of <see cref="AuthService"/>.</summary>
    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IReferralRepository referralRepository,
        ITokenService tokenService,
        IEmailService emailService,
        ICacheService cacheService,
        IMapper mapper,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _referralRepository = referralRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _cacheService = cacheService;
        _mapper = mapper;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <summary>Registers a new user and sends an email verification link.</summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
            throw new ConflictException("An account with this email already exists.");

        // Validate referral code if provided
        User? referrer = null;
        if (!string.IsNullOrEmpty(request.ReferralCode))
        {
            referrer = await _userRepository.GetByReferralCodeAsync(request.ReferralCode, cancellationToken);
            if (referrer is null)
                throw new ValidationException("Invalid referral code.");
        }

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            EmailNormalized = request.Email.ToUpperInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Status = UserStatus.PendingVerification,
            ReferralCode = GenerateReferralCode(),
            ReferredById = referrer?.Id,
            EmailVerificationToken = GenerateSecureToken(),
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(AppConstants.EmailVerificationTokenExpiryHours)
        };

        // Assign default User role (done via seed data / role assignment flow)
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Process referral
        if (referrer is not null)
        {
            var referral = new Referral
            {
                ReferrerId = referrer.Id,
                ReferredUserId = user.Id,
                ReferralCode = request.ReferralCode!
            };
            await _referralRepository.AddAsync(referral, cancellationToken);
            await _referralRepository.SaveChangesAsync(cancellationToken);
        }

        // Send verification email (fire-and-forget)
        _ = _emailService.SendEmailVerificationAsync(user.Email, user.FullName, user.EmailVerificationToken, cancellationToken)
            .ContinueWith(t => _logger.LogWarning(t.Exception, "Failed to send verification email"), TaskContinuationOptions.OnlyOnFaulted);

        // Return auth tokens (allow immediate use)
        return await BuildAuthResponseAsync(user, cancellationToken: cancellationToken);
    }

    /// <summary>Authenticates a user and returns JWT + refresh tokens.</summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetWithRolesAsync(
            (await _userRepository.GetByEmailAsync(request.Email, cancellationToken))?.Id ?? Guid.Empty,
            cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (user.IsLockedOut)
            throw new UnauthorizedException($"Account is locked until {user.LockoutEnd:HH:mm UTC}. Please try again later.");

        if (user.Status == UserStatus.Suspended)
            throw new ForbiddenException("Your account has been suspended. Please contact support.");

        // Reset failed attempts on success
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} logged in from {Ip}", user.Id, ipAddress);
        return await BuildAuthResponseAsync(user, ipAddress, request.DeviceInfo, cancellationToken: cancellationToken);
    }

    /// <summary>Refreshes JWT tokens using a valid refresh token.</summary>
    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (refreshToken is null || !refreshToken.IsActive)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var user = await _userRepository.GetWithRolesAsync(refreshToken.UserId, cancellationToken);
        if (user is null) throw new NotFoundException("User", refreshToken.UserId);

        // Rotate refresh token
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByToken = newRefreshTokenValue;
        refreshToken.RevokedReason = "Replaced by new token";
        _refreshTokenRepository.Update(refreshToken);

        var newRefreshToken = CreateRefreshToken(user.Id, newRefreshTokenValue, ipAddress, request.RefreshToken);
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        // Invalidate user cache
        await _cacheService.RemoveAsync(CacheKeys.UserPermissions(user.Id), cancellationToken);

        return await BuildAuthResponseAsync(user, ipAddress, existingRefreshToken: newRefreshTokenValue, cancellationToken: cancellationToken);
    }

    /// <summary>Revokes a refresh token to log out the user.</summary>
    public async Task LogoutAsync(LogoutRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (refreshToken is null || refreshToken.UserId != userId)
            throw new UnauthorizedException("Invalid token.");

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedReason = "User logout";
        _refreshTokenRepository.Update(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync(CacheKeys.UserPermissions(userId), cancellationToken);
        _logger.LogInformation("User {UserId} logged out.", userId);
    }

    /// <summary>Changes the authenticated user's password.</summary>
    public async Task ChangePasswordAsync(ChangePasswordRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.SetUpdated(userId);
        _userRepository.Update(user);

        // Revoke all refresh tokens for security
        await _refreshTokenRepository.RevokeAllForUserAsync(userId, "Password changed", cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} changed their password.", userId);
    }

    /// <summary>Initiates a password reset flow by sending a reset email.</summary>
    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null) return; // Silent fail to prevent user enumeration

        user.PasswordResetToken = GenerateSecureToken();
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(AppConstants.PasswordResetTokenExpiryHours);
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        await _emailService.SendPasswordResetAsync(user.Email, user.FullName, user.PasswordResetToken, cancellationToken);
    }

    /// <summary>Completes a password reset using a valid token.</summary>
    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindFirstAsync(
            u => u.PasswordResetToken == request.Token && u.PasswordResetTokenExpiry > DateTime.UtcNow,
            cancellationToken) ?? throw new ValidationException("Invalid or expired password reset token.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        _userRepository.Update(user);

        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, "Password reset", cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Verifies a user's email address using the verification token.</summary>
    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindFirstAsync(
            u => u.EmailVerificationToken == request.Token && u.EmailVerificationTokenExpiry > DateTime.UtcNow,
            cancellationToken) ?? throw new ValidationException("Invalid or expired email verification token.");

        user.IsEmailVerified = true;
        user.Status = UserStatus.Active;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _ = _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, cancellationToken);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<AuthResponse> BuildAuthResponseAsync(
        User user,
        string? ipAddress = null,
        string? deviceInfo = null,
        string? existingRefreshToken = null,
        CancellationToken cancellationToken = default)
    {
        var roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? [];
        var permissions = user.UserRoles?
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList() ?? [];

        var accessToken = await _tokenService.GenerateAccessTokenAsync(user.Id, user.Email, roles, permissions);
        var refreshTokenValue = existingRefreshToken ?? _tokenService.GenerateRefreshToken();

        if (existingRefreshToken is null)
        {
            var refreshToken = CreateRefreshToken(user.Id, refreshTokenValue, ipAddress, deviceInfo);
            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        }

        var userSummary = _mapper.Map<UserSummaryDto>(user);
        var expiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes);

        return new AuthResponse(accessToken, refreshTokenValue, expiry, userSummary);
    }

    private RefreshToken CreateRefreshToken(Guid userId, string token, string? ipAddress, string? deviceInfo) =>
        new()
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            CreatedByIp = ipAddress,
            DeviceInfo = deviceInfo
        };

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[48];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string GenerateReferralCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, AppConstants.ReferralCodeLength).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
