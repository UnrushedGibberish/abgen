using AbGen.Api.Domain.Jobs;
using AbGen.Api.Application.Jobs;
using AbGen.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AbGen.Api.Infrastructure.Jobs;

public sealed class EfJobRepository(JobsDbContext dbContext, ISystemClock clock) : IJobRepository
{
    public async Task<JobRecord> EnqueueAsync(JobRecord job, CancellationToken cancellationToken)
    {
        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<IReadOnlyList<JobRecord>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Jobs.AsNoTracking()
            .Include(job => job.Events.OrderBy(jobEvent => jobEvent.SequenceNumber))
            .OrderByDescending(job => job.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<JobRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Jobs.AsNoTracking()
            .Include(job => job.Events.OrderBy(jobEvent => jobEvent.SequenceNumber))
            .SingleOrDefaultAsync(job => job.Id == id, cancellationToken);

    public Task<JobRecord?> GetForProcessingAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Jobs
            .Include(job => job.Events.OrderBy(jobEvent => jobEvent.SequenceNumber))
            .SingleOrDefaultAsync(job => job.Id == id, cancellationToken);

    public Task<JobRecord?> GetNextQueuedAsync(CancellationToken cancellationToken) =>
        dbContext.Jobs.AsNoTracking()
            .Where(job => job.Status == JobStatus.Queued)
            .OrderBy(job => job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task RequeueInterruptedAsync(CancellationToken cancellationToken)
    {
        var interruptedJobs = await dbContext.Jobs
            .Include(job => job.Events.OrderBy(jobEvent => jobEvent.SequenceNumber))
            .Where(job => job.Status == JobStatus.Running && job.AttemptStartedAt != null)
            .ToListAsync(cancellationToken);
        var now = clock.UtcNow;

        foreach (var job in interruptedJobs)
        {
            job.RequeueAfterInterruptedAttempt(now);
            job.AddEvent(
                JobEventLevel.Warning,
                "job.recovered",
                "The API restarted during this job. Its latest attempt will be retried.",
                now);
        }

        if (interruptedJobs.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

    }

    public async Task<bool> TryBeginAttemptAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.BeginTransactionIfRelationalAsync(cancellationToken);
        var job = await dbContext.Jobs
            .Include(candidate => candidate.Events.OrderBy(jobEvent => jobEvent.SequenceNumber))
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (job is null || job.Status != JobStatus.Queued)
        {
            return false;
        }

        job.MarkRunning(clock.UtcNow);
        job.AddEvent(JobEventLevel.Information, "job.started", "Job processing started.", clock.UtcNow);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public Task UpdateAsync(JobRecord job, CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
