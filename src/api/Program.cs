using System.Text.Json.Serialization;
using AbGen.Api;
using AbGen.Api.Application.Jobs;
using AbGen.Api.Domain.Jobs;
using AbGen.Api.Infrastructure.Jobs;
using AbGen.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--healthcheck")
        {
            await CheckHealthAsync(args[1]);
            return;
        }

        var app = CreateApplication(args);
        await EnsureDatabaseAsync(app);
        await app.RunAsync();
    }

    private static WebApplication CreateApplication(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder);

        var app = builder.Build();
        ConfigureMiddleware(app);
        MapEndpoints(app);
        return app;
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres must be configured.");

        builder.Services.AddDbContext<JobsDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddHealthChecks()
            .AddCheck("liveness", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<PostgresReadinessCheck>("postgresql", tags: ["ready"]);
        builder.Services.AddScoped<IJobRepository, EfJobRepository>();
        builder.Services.AddScoped<JobApplicationService>();
        builder.Services.AddSingleton<ISystemClock, SystemClock>();
        builder.Services.AddHostedService<JobProcessingService>();
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
            .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()));
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseCors("frontend");
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live") || check.Tags.Contains("ready")
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
    }

    private static void MapEndpoints(WebApplication app)
    {
        var jobs = app.MapGroup("/api/jobs");
        jobs.MapGet("", async (JobApplicationService service, CancellationToken cancellationToken) =>
        {
            var storedJobs = await service.GetAllAsync(cancellationToken);
            return Results.Ok(storedJobs.Select(ToResponse));
        });
        jobs.MapGet("/{id:guid}", async (Guid id, JobApplicationService service, CancellationToken cancellationToken) =>
        {
            var job = await service.GetByIdAsync(id, cancellationToken);
            return job is null ? Results.NotFound() : Results.Ok(ToResponse(job));
        });
    }

    private static async Task CheckHealthAsync(string path)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        try
        {
            using var response = await client.GetAsync($"http://127.0.0.1:8080{path}");
            if (!response.IsSuccessStatusCode)
            {
                Environment.ExitCode = 1;
            }
        }
        catch (HttpRequestException)
        {
            Environment.ExitCode = 1;
        }
        catch (TaskCanceledException)
        {
            Environment.ExitCode = 1;
        }
    }

    private static JobResponse ToResponse(JobRecord job) => new(
        job.Id,
        job.JobType,
        job.Status,
        job.ProgressPercent,
        job.CurrentStep,
        job.CreatedAt,
        job.UpdatedAt,
        job.StartedAt,
        job.CompletedAt,
        job.AttemptCount,
        job.ErrorCode,
        job.ErrorMessage,
        job.Events.Select(jobEvent => new JobEventResponse(
            jobEvent.SequenceNumber,
            jobEvent.Level,
            jobEvent.Code,
            jobEvent.Message,
            jobEvent.OccurredAt)).ToArray());

    private static async Task EnsureDatabaseAsync(WebApplication app)
    {
        const int maxAttempts = 30;
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");

        for (var attempt = 1; ; attempt++)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<JobsDbContext>();

            try
            {
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migrations are up to date.");
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                logger.LogWarning(exception, "Database unavailable during migration (attempt {Attempt}/{MaxAttempts}); retrying shortly.", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, "Database migrations could not be applied. The API will not start until schema is ready.");
                throw;
            }
        }
    }
}

public sealed record JobResponse(
    Guid Id,
    string JobType,
    JobStatus Status,
    int ProgressPercent,
    string CurrentStep,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int AttemptCount,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<JobEventResponse> Events);

public sealed record JobEventResponse(
    int SequenceNumber,
    JobEventLevel Level,
    string Code,
    string Message,
    DateTimeOffset OccurredAt);
