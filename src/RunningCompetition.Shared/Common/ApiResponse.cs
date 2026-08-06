namespace RunningCompetition.Shared.Common;

/// <summary>
/// Generic API response envelope used for all endpoints.
/// </summary>
/// <typeparam name="T">The type of the payload.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>Gets a value indicating whether the request was successful.</summary>
    public bool Success { get; init; }

    /// <summary>Gets the HTTP status code.</summary>
    public int StatusCode { get; init; }

    /// <summary>Gets a human-readable message.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Gets the response payload.</summary>
    public T? Data { get; init; }

    /// <summary>Gets the list of validation or error details.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Gets the unique request trace ID for debugging.</summary>
    public string TraceId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Gets the UTC timestamp of the response.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Creates a successful response.</summary>
    public static ApiResponse<T> Ok(T data, string message = "Success", int statusCode = 200) =>
        new() { Success = true, StatusCode = statusCode, Message = message, Data = data };

    /// <summary>Creates a created response.</summary>
    public static ApiResponse<T> Created(T data, string message = "Created successfully") =>
        new() { Success = true, StatusCode = 201, Message = message, Data = data };

    /// <summary>Creates a failure response.</summary>
    public static ApiResponse<T> Fail(string message, int statusCode = 400, IEnumerable<string>? errors = null) =>
        new() { Success = false, StatusCode = statusCode, Message = message, Errors = errors?.ToList().AsReadOnly() ?? [] };

    /// <summary>Creates a not found response.</summary>
    public static ApiResponse<T> NotFound(string message = "Resource not found") =>
        Fail(message, 404);

    /// <summary>Creates an unauthorized response.</summary>
    public static ApiResponse<T> Unauthorized(string message = "Unauthorized") =>
        Fail(message, 401);

    /// <summary>Creates a forbidden response.</summary>
    public static ApiResponse<T> Forbidden(string message = "Forbidden") =>
        Fail(message, 403);

    /// <summary>Creates an internal server error response.</summary>
    public static ApiResponse<T> ServerError(string message = "An unexpected error occurred") =>
        Fail(message, 500);
}

/// <summary>
/// Non-generic API response for operations that return no data.
/// </summary>
public sealed class ApiResponse
{
    /// <summary>Gets a value indicating whether the request was successful.</summary>
    public bool Success { get; init; }

    /// <summary>Gets the HTTP status code.</summary>
    public int StatusCode { get; init; }

    /// <summary>Gets a human-readable message.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Gets the list of errors.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Gets the unique request trace ID.</summary>
    public string TraceId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Gets the UTC timestamp.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Creates a successful response.</summary>
    public static ApiResponse Ok(string message = "Success") =>
        new() { Success = true, StatusCode = 200, Message = message };

    /// <summary>Creates a failure response.</summary>
    public static ApiResponse Fail(string message, int statusCode = 400, IEnumerable<string>? errors = null) =>
        new() { Success = false, StatusCode = statusCode, Message = message, Errors = errors?.ToList().AsReadOnly() ?? [] };
}
