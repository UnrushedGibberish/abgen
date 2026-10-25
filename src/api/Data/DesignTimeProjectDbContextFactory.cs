using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AbGen.Api.Data;

public sealed class DesignTimeProjectDbContextFactory : IDesignTimeDbContextFactory<ProjectDbContext>
{
    public ProjectDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=abgen;Username=abgen;******";
        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new ProjectDbContext(options);
    }
}
