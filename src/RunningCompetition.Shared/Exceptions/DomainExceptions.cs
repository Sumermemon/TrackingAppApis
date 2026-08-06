namespace RunningCompetition.Shared.Exceptions;

/// <summary>Thrown when a requested resource is not found.</summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Initializes a <see cref="NotFoundException"/> with a resource name and identifier.</summary>
    public NotFoundException(string name, object key)
        : base($"Entity '{name}' with key '{key}' was not found.") { }

    /// <summary>Initializes a <see cref="NotFoundException"/> with a custom message.</summary>
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Thrown when input validation fails.</summary>
public sealed class ValidationException : Exception
{
    /// <summary>Gets the collection of validation errors keyed by property name.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>Initializes a <see cref="ValidationException"/> with an errors dictionary.</summary>
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors).AsReadOnly();
    }

    /// <summary>Initializes a <see cref="ValidationException"/> with a simple message.</summary>
    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>().AsReadOnly();
    }
}

/// <summary>Thrown when the user is not authorized to perform an action.</summary>
public sealed class ForbiddenException : Exception
{
    /// <summary>Initializes a <see cref="ForbiddenException"/>.</summary>
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message) { }
}

/// <summary>Thrown when authentication fails.</summary>
public sealed class UnauthorizedException : Exception
{
    /// <summary>Initializes a <see cref="UnauthorizedException"/>.</summary>
    public UnauthorizedException(string message = "Authentication required.") : base(message) { }
}

/// <summary>Thrown when a business rule is violated.</summary>
public sealed class BusinessRuleException : Exception
{
    /// <summary>Initializes a <see cref="BusinessRuleException"/>.</summary>
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>Thrown when a conflict exists (e.g. duplicate entry).</summary>
public sealed class ConflictException : Exception
{
    /// <summary>Initializes a <see cref="ConflictException"/>.</summary>
    public ConflictException(string message) : base(message) { }
}
