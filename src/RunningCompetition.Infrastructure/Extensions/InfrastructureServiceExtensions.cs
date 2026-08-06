using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RunningCompetition.Contracts.Services;
using RunningCompetition.Infrastructure.AI;
using RunningCompetition.Infrastructure.Auth;
using RunningCompetition.Infrastructure.Cache;
using RunningCompetition.Infrastructure.Email;
using RunningCompetition.Infrastructure.Hubs;
using RunningCompetition.Infrastructure.Jobs;
using RunningCompetition.Infrastructure.Notifications;
using RunningCompetition.Shared.Settings;

namespace RunningCompetition.Infrastructure.Extensions;

/// <summary>Registers all Infrastructure services in the DI container.</summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Adds JWT authentication, Redis cache, Hangfire, SignalR,
    /// and all infrastructure service implementations.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Settings
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.Configure<RedisSettings>(configuration.GetSection("Redis"));
        services.Configure<HangfireSettings>(configuration.GetSection("Hangfire"));

        // HTTP Context
        services.AddHttpContextAccessor();

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<IAiCoachService, AiCoachService>();
        services.AddScoped<RunHubService>();

        // JWT Authentication
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()!;
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            // Allow JWT in SignalR query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        context.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        });

        // Authorization
        services.AddAuthorization();

        // Redis Cache
        var redisSettings = configuration.GetSection("Redis").Get<RedisSettings>()!;
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisSettings.ConnectionString;
            options.InstanceName = redisSettings.InstanceName;
        });

        // Hangfire
        var pgConn = configuration.GetConnectionString("DefaultConnection")!;
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(pgConn)));
        services.AddHangfireServer(options =>
        {
            var hangfireSettings = configuration.GetSection("Hangfire").Get<HangfireSettings>()!;
            options.WorkerCount = hangfireSettings.WorkerCount;
        });
        services.AddScoped<BackgroundJobService>();

        // SignalR
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = false;
            options.MaximumReceiveMessageSize = 32 * 1024;
        });

        return services;
    }
}
