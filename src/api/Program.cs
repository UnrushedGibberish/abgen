using AbGen.Api;
using AbGen.Api.Controllers;
using AbGen.Api.Data;
using AbGen.Api.Data.HealthChecks;
using AbGen.Api.Data.Repositories;
using AbGen.Api.Services;
using Microsoft.AspNetCore.OpenApi;
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
        app.MapProjectEndpoints();
        return app;
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres must be configured.");

        builder.Services.AddDbContext<ProjectDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddHealthChecks()
            .AddCheck("liveness", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<PostgresReadinessCheck>("postgresql", tags: ["ready"]);
        builder.Services.AddScoped<IProjectRepository, EfProjectRepository>();
        builder.Services.AddScoped<IProjectService, ProjectService>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddValidation();
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
            .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()));
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseCors("frontend");
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

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

    private static async Task EnsureDatabaseAsync(WebApplication app)
    {
        const int maxAttempts = 30;
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");

        for (var attempt = 1; ; attempt++)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

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
