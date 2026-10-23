using AbGen.Api.Domain.Jobs;
using AbGen.Api.Infrastructure.Jobs;
using AbGen.Api.Application.Jobs;

namespace AbGen.Tests.Jobs;

public sealed class JobApplicationServiceTests
{
    [Fact]
    public async Task Enqueue_placeholder_saves_a_durable_job_and_status_can_be_read_again()
    {
        var repository = new InMemoryJobRepository();
        var clock = new FixedClock(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        var service = new JobApplicationService(repository, clock);

        var acceptedJob = await service.EnqueuePlaceholderAsync(CancellationToken.None);
        var restoredJob = await service.GetByIdAsync(acceptedJob.Id, CancellationToken.None);

        Assert.NotNull(restoredJob);
        Assert.Equal(acceptedJob.Id, restoredJob.Id);
        Assert.Equal(JobStatus.Queued, restoredJob.Status);
        Assert.Equal("job.queued", Assert.Single(restoredJob.Events).Code);
        Assert.Single(await service.GetAllAsync(CancellationToken.None));
    }

    private sealed class InMemoryJobRepository : IJobRepository
    {
        private readonly Dictionary<Guid, JobRecord> _jobs = [];

        public Task<JobRecord> EnqueueAsync(JobRecord job, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _jobs.Add(job.Id, job);
            return Task.FromResult(job);
        }

        public Task<IReadOnlyList<JobRecord>> GetAllAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<JobRecord> jobs = _jobs.Values.ToArray();
            return Task.FromResult(jobs);
        }

        public Task<JobRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _jobs.TryGetValue(id, out var job);
            return Task.FromResult(job);
        }

        public Task<JobRecord?> GetForProcessingAsync(Guid id, CancellationToken cancellationToken) => GetByIdAsync(id, cancellationToken);

        public Task<JobRecord?> GetNextQueuedAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_jobs.Values.FirstOrDefault(job => job.Status == JobStatus.Queued));
        }

        public Task RequeueInterruptedAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task<bool> TryBeginAttemptAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_jobs.TryGetValue(id, out var job) || job.Status != JobStatus.Queued)
            {
                return Task.FromResult(false);
            }

            job.MarkRunning(DateTimeOffset.UtcNow);
            return Task.FromResult(true);
        }

        public Task UpdateAsync(JobRecord job, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
