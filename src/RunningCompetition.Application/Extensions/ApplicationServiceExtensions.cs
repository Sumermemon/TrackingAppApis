using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RunningCompetition.Application.Mappings;
using RunningCompetition.Application.Services;
using RunningCompetition.Application.Validators;

namespace RunningCompetition.Application.Extensions;

/// <summary>Registers Application layer services in the DI container.</summary>
public static class ApplicationServiceExtensions
{
    /// <summary>Adds application services, AutoMapper, and FluentValidation.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        // FluentValidation — scan from validator assembly
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        // Application Services
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<RunService>();
        services.AddScoped<SocialService>();
        services.AddScoped<ReferralService>();
        services.AddScoped<AchievementService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<LeaderboardService>();
        services.AddScoped<AdminService>();

        return services;
    }
}
