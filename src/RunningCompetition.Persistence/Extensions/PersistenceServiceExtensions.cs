using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunningCompetition.Contracts.Repositories;
using RunningCompetition.Persistence.Context;
using RunningCompetition.Persistence.Repositories;
using RunningCompetition.Persistence.Seeds;

namespace RunningCompetition.Persistence.Extensions;

/// <summary>Registers Persistence layer services in the DI container.</summary>
public static class PersistenceServiceExtensions
{
    /// <summary>
    /// Adds the EF Core DbContext (PostgreSQL), all repositories, and runs migrations/seed on startup.
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        // DbContext
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsql.CommandTimeout(60);
                npgsql.EnableRetryOnFailure(3);
            });
            options.UseSnakeCaseNamingConvention();
        });

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRunSessionRepository, RunSessionRepository>();
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IReferralRepository, ReferralRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();

        return services;
    }

    /// <summary>
    /// Runs database migrations and seed data on application startup.
    /// Call this from the app's startup pipeline.
    /// </summary>
    public static async Task MigrateAndSeedAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        await DbSeeder.SeedAsync(context);
    }
}
