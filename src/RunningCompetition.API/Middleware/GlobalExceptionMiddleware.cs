using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RunningCompetition.Shared.Common;
using RunningCompetition.Shared.Exceptions;

namespace RunningCompetition.API.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions
/// and converts them to a consistent <see cref="ApiResponse{T}"/> envelope.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>Initializes a new instance of <see cref="GlobalExceptionMiddleware"/>.</summary>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Processes the request and catches any unhandled exceptions.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            NotFoundException nfe => (HttpStatusCode.NotFound, nfe.Message, (IEnumerable<string>)[]),
            ValidationException ve => (HttpStatusCode.UnprocessableEntity, ve.Message,
                ve.Errors.SelectMany(e => e.Value.Select(v => $"{e.Key}: {v}"))),
            UnauthorizedException ue => (HttpStatusCode.Unauthorized, ue.Message, (IEnumerable<string>)[]),
            ForbiddenException fe => (HttpStatusCode.Forbidden, fe.Message, (IEnumerable<string>)[]),
            ConflictException ce => (HttpStatusCode.Conflict, ce.Message, (IEnumerable<string>)[]),
            BusinessRuleException bre => (HttpStatusCode.BadRequest, bre.Message, (IEnumerable<string>)[]),
            FluentValidation.ValidationException fve => (HttpStatusCode.UnprocessableEntity, "Validation failed",
                fve.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (IEnumerable<string>)[])
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(exception, "Handled exception [{StatusCode}] on {Method} {Path}", (int)statusCode, context.Request.Method, context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(message, (int)statusCode, errors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
