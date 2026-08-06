using FluentValidation;
using RunningCompetition.Application.DTOs.Auth;
using RunningCompetition.Application.DTOs.Runs;
using RunningCompetition.Application.DTOs.Users;
using RunningCompetition.Application.DTOs;

namespace RunningCompetition.Application.Validators;

/// <summary>Validator for <see cref="RegisterRequest"/>.</summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).WithMessage("First name is required and must not exceed 100 characters.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).WithMessage("Last name is required and must not exceed 100 characters.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256).WithMessage("A valid email is required.");
        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.ReferralCode).MaximumLength(20).When(x => x.ReferralCode is not null);
    }
}

/// <summary>Validator for <see cref="LoginRequest"/>.</summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

/// <summary>Validator for <see cref="ChangePasswordRequest"/>.</summary>
public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

/// <summary>Validator for <see cref="ForgotPasswordRequest"/>.</summary>
public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

/// <summary>Validator for <see cref="ResetPasswordRequest"/>.</summary>
public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

/// <summary>Validator for <see cref="UpdateProfileRequest"/>.</summary>
public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.HeightCm).InclusiveBetween(50, 300).When(x => x.HeightCm.HasValue);
        RuleFor(x => x.WeightKg).InclusiveBetween(20, 500).When(x => x.WeightKg.HasValue);
        RuleFor(x => x.DateOfBirth).LessThan(DateOnly.FromDateTime(DateTime.UtcNow)).When(x => x.DateOfBirth.HasValue);
        RuleFor(x => x.Country).Length(2).When(x => x.Country is not null);
    }
}

/// <summary>Validator for <see cref="GpsBatchRequest"/>.</summary>
public sealed class GpsBatchRequestValidator : AbstractValidator<GpsBatchRequest>
{
    public GpsBatchRequestValidator()
    {
        RuleFor(x => x.Locations).NotEmpty().Must(l => l.Count <= 100).WithMessage("Cannot upload more than 100 GPS points per batch.");
        RuleForEach(x => x.Locations).SetValidator(new GpsLocationRequestValidator());
    }
}

/// <summary>Validator for <see cref="GpsLocationRequest"/>.</summary>
public sealed class GpsLocationRequestValidator : AbstractValidator<GpsLocationRequest>
{
    public GpsLocationRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Sequence).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Timestamp).LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5));
    }
}

/// <summary>Validator for <see cref="CreateRoleRequest"/>.</summary>
public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.PermissionIds).NotNull();
    }
}

/// <summary>Validator for <see cref="CreateAnnouncementRequest"/>.</summary>
public sealed class CreateAnnouncementRequestValidator : AbstractValidator<CreateAnnouncementRequest>
{
    public CreateAnnouncementRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTime.UtcNow).When(x => x.ExpiresAt.HasValue);
    }
}
