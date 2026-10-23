using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AbGen.Api.Infrastructure.Persistence;

public sealed class DesignTimeJobsDbContextFactory : IDesignTimeDbContextFactory<JobsDbContext>
{
    public JobsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=abgen;Username=abgen;Password=development-only";
        var options = new DbContextOptionsBuilder<JobsDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new JobsDbContext(options);
    }
}