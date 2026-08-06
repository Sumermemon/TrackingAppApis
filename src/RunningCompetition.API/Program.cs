using System.IO.Compression;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Hangfire;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.OpenApi;
using RunningCompetition.API.Middleware;
using RunningCompetition.Application.Extensions;
using RunningCompetition.Infrastructure.Extensions;
using RunningCompetition.Infrastructure.Hubs;
using RunningCompetition.Infrastructure.Jobs;
using RunningCompetition.Persistence.Extensions;
using Serilog;
using Serilog.Events;

// ── Bootstrap Serilog ─────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Running Competition API");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog (full) ────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

    // ── Configuration ─────────────────────────────────────────────────────────
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

    // ── Application Layers ────────────────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddPersistence(connectionString);

    // ── Controllers + JSON ────────────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    // ── API Versioning ────────────────────────────────────────────────────────
    builder.Services.AddApiVersioning(opts =>
    {
        opts.DefaultApiVersion = new ApiVersion(1, 0);
        opts.AssumeDefaultVersionWhenUnspecified = true;
        opts.ReportApiVersions = true;
        opts.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version"));
    }).AddApiExplorer(opts =>
    {
        opts.GroupNameFormat = "'v'VVV";
        opts.SubstituteApiVersionInUrl = true;
    });

    // ── OpenAPI ───────────────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();

    // ── Rate Limiting ─────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(opts =>
    {
        opts.RejectionStatusCode = 429;

        // Default sliding window: 100 req / 60 sec
        opts.AddSlidingWindowLimiter("default", o =>
        {
            o.PermitLimit = 100;
            o.Window = TimeSpan.FromSeconds(60);
            o.SegmentsPerWindow = 6;
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            o.QueueLimit = 10;
        });

        // Auth endpoints: stricter — 10 req / 60 sec
        opts.AddSlidingWindowLimiter("auth", o =>
        {
            o.PermitLimit = 10;
            o.Window = TimeSpan.FromSeconds(60);
            o.SegmentsPerWindow = 6;
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            o.QueueLimit = 0;
        });

        // GPS upload: 300 req / 60 sec
        opts.AddSlidingWindowLimiter("gps", o =>
        {
            o.PermitLimit = 300;
            o.Window = TimeSpan.FromSeconds(60);
            o.SegmentsPerWindow = 6;
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            o.QueueLimit = 20;
        });
    });

    // ── Response Compression ──────────────────────────────────────────────────
    builder.Services.AddResponseCompression(opts =>
    {
        opts.EnableForHttps = true;
        opts.Providers.Add<BrotliCompressionProvider>();
        opts.Providers.Add<GzipCompressionProvider>();
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(opts => opts.Level = CompressionLevel.Fastest);
    builder.Services.Configure<GzipCompressionProviderOptions>(opts => opts.Level = CompressionLevel.Fastest);

    // ── Health Checks ─────────────────────────────────────────────────────────
    var redisConn = builder.Configuration["Redis:ConnectionString"]!;
    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "postgresql", tags: ["db"])
        .AddRedis(redisConn, name: "redis", tags: ["cache"]);

    // ── CORS ──────────────────────────────────────────────────────────────────
    builder.Services.AddCors(opts => opts.AddPolicy("AllowAll", p => p
        .AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    // ── Build App ─────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Migrate & Seed ────────────────────────────────────────────────────────
    await app.Services.MigrateAndSeedAsync();

    // ── Middleware Pipeline ───────────────────────────────────────────────────
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        opts.GetLevel = (ctx, elapsed, ex) =>
            ex is not null || ctx.Response.StatusCode >= 500 ? LogEventLevel.Error : LogEventLevel.Information;
    });

    app.UseResponseCompression();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opts =>
        {
            opts.WithTitle("Running Competition API")
                .WithTheme(ScalarTheme.DeepSpace)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }

    app.UseCors("AllowAll");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // Audit middleware (after auth so user identity is available)
    app.UseMiddleware<AuditMiddleware>();

    app.MapControllers().RequireRateLimiting("default");

    // ── SignalR Hubs ──────────────────────────────────────────────────────────
    app.MapHub<RunHub>("/hubs/run").RequireAuthorization();

    // ── Hangfire Dashboard ────────────────────────────────────────────────────
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [] // TODO: add auth filter for production
    });

    // ── Health Checks ─────────────────────────────────────────────────────────
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = hc => hc.Tags.Contains("db") || hc.Tags.Contains("cache"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    // ── Register Recurring Hangfire Jobs ──────────────────────────────────────
    BackgroundJobService.RegisterRecurringJobs();

    Log.Information("Running Competition API started successfully");
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
