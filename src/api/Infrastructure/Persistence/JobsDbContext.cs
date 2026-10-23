using AbGen.Api.Domain.Jobs;
using AbGen.Api.Application.Jobs;
using Microsoft.EntityFrameworkCore;

namespace AbGen.Api.Infrastructure.Persistence;

public sealed class JobsDbContext(DbContextOptions<JobsDbContext> options) : DbContext(options)
{
    public DbSet<JobRecord> Jobs => Set<JobRecord>();
    public DbSet<JobEventRecord> JobEvents => Set<JobEventRecord>();

    public async Task<JobTransaction> BeginTransactionIfRelationalAsync(CancellationToken cancellationToken)
    {
        var transaction = Database.IsRelational()
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;
        return new JobTransaction(transaction);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var jobs = modelBuilder.Entity<JobRecord>();
        jobs.ToTable("jobs");
        jobs.HasKey(job => job.Id);
        jobs.Property(job => job.JobType).HasMaxLength(64).IsRequired();
        jobs.Property(job => job.Payload).HasColumnType("jsonb").IsRequired();
        jobs.Property(job => job.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        jobs.Property(job => job.CurrentStep).HasMaxLength(160).IsRequired();
        jobs.Property(job => job.ErrorCode).HasMaxLength(100);
        jobs.Property(job => job.ErrorMessage).HasMaxLength(2000);
        jobs.Property(job => job.AttemptCount).IsConcurrencyToken();
        jobs.Property(job => job.CreatedAt).HasColumnType("timestamp with time zone");
        jobs.Property(job => job.UpdatedAt).HasColumnType("timestamp with time zone");
        jobs.Property(job => job.StartedAt).HasColumnType("timestamp with time zone");
        jobs.Property(job => job.AttemptStartedAt).HasColumnType("timestamp with time zone");
        jobs.Property(job => job.CompletedAt).HasColumnType("timestamp with time zone");
        jobs.HasIndex(job => new { job.Status, job.CreatedAt });
        jobs.HasMany(job => job.Events).WithOne().HasForeignKey(jobEvent => jobEvent.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        var events = modelBuilder.Entity<JobEventRecord>();
        events.ToTable("job_events");
        events.HasKey(jobEvent => new { jobEvent.JobId, jobEvent.SequenceNumber });
        events.Property(jobEvent => jobEvent.Level).HasConversion<string>().HasMaxLength(16).IsRequired();
        events.Property(jobEvent => jobEvent.Code).HasMaxLength(64).IsRequired();
        events.Property(jobEvent => jobEvent.Message).HasMaxLength(2000).IsRequired();
        events.Property(jobEvent => jobEvent.OccurredAt).HasColumnType("timestamp with time zone");
        base.OnModelCreating(modelBuilder);
    }
}
