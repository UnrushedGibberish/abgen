using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AbGen.Api.Data;

public sealed class DesignTimeProjectDbContextFactory : IDesignTimeDbContextFactory<ProjectDbContext>
{
    public ProjectDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? BuildLocalConnectionString();
        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new ProjectDbContext(options);
    }

    private static string BuildLocalConnectionString()
    {
        var port = Environment.GetEnvironmentVariable("ABGEN_POSTGRES_PORT") ?? "5432";
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "development-only";
        return $"Host=127.0.0.1;Port={port};Database=abgen;Username=abgen;Password={password}";
    }
}
