using AbGen.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AbGen.Api.Data;

public sealed class ProjectDbContext(DbContextOptions<ProjectDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var projects = modelBuilder.Entity<Project>();
        projects.ToTable("projects");
        projects.HasKey(project => project.Id);
        projects.Property(project => project.Name)
            .HasMaxLength(Project.MaximumNameLength)
            .IsRequired();
        projects.Property(project => project.CreatedAt).HasColumnType("timestamp with time zone");
        projects.Property(project => project.UpdatedAt).HasColumnType("timestamp with time zone");
        projects.HasIndex(project => project.CreatedAt);
        base.OnModelCreating(modelBuilder);
    }
}
