using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RunningCompetition.Contracts.Services;
using RunningCompetition.Domain.Entities;
using RunningCompetition.Domain.Enums;
using RunningCompetition.Persistence.Context;

namespace RunningCompetition.API.Middleware;

/// <summary>
/// Middleware that writes structured audit log entries for state-changing API operations.
/// Logs POST, PUT, PATCH, DELETE requests that succeed (2xx).
/// </summary>
public sealed class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser, AppDbContext dbContext)
    {
        await _next(context);

        var method = context.Request.Method;
        if (!IsAuditableMethod(method)) return;
        if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300) return;
        if (!currentUser.IsAuthenticated) return;

        var action = method switch
        {
            "POST" => AuditAction.Create,
            "PUT" or "PATCH" => AuditAction.Update,
            "DELETE" => AuditAction.Delete,
            _ => AuditAction.Update
        };

        var log = new AuditLog
        {
            UserId = currentUser.UserId,
            UserEmail = currentUser.Email,
            Action = action,
            EntityType = context.Request.Path.Value ?? "Unknown",
            IpAddress = currentUser.IpAddress,
            UserAgent = context.Request.Headers.UserAgent.ToString().AsSpan()[..Math.Min(500, context.Request.Headers.UserAgent.ToString().Length)].ToString()
        };

        dbContext.AuditLogs.Add(log);
        await dbContext.SaveChangesAsync();
    }

    private static bool IsAuditableMethod(string method) =>
        method is "POST" or "PUT" or "PATCH" or "DELETE";
}
