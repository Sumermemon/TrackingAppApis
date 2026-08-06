using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RunningCompetition.Contracts.Services;
using RunningCompetition.Shared.Constants;

namespace RunningCompetition.Infrastructure.Auth;

/// <summary>
/// Provides the current authenticated user's identity from the HttpContext.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new instance of <see cref="CurrentUserService"/>.</summary>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var claim = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Principal?.FindFirst("sub")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public string? Email => Principal?.FindFirst(ClaimTypes.Email)?.Value
                         ?? Principal?.FindFirst("email")?.Value;

    /// <inheritdoc />
    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList().AsReadOnly() ?? [];

    /// <inheritdoc />
    public IReadOnlyList<string> Permissions =>
        Principal?.FindAll("permission").Select(c => c.Value).ToList().AsReadOnly() ?? [];

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public bool IsSuperAdmin => Roles.Contains(RoleNames.SuperAdmin);

    /// <inheritdoc />
    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <inheritdoc />
    public bool HasPermission(string permission) => Permissions.Contains(permission);
}
