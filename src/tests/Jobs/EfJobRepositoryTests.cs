using AbGen.Api.Domain.Jobs;
using AbGen.Api.Infrastructure.Jobs;
using AbGen.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AbGen.Tests.Jobs;

public sealed class EfJobRepositoryTests
{
    [Fact]
    public async Task Job_events_and_recovery_status_roundtrip_through_database()
    {
        var options = new DbContextOptionsBuilder<JobsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var now = new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
        var jobId = Guid.NewGuid();

        await using (var dbContext = new JobsDbContext(options))
        {
            var repository = new EfJobRepository(dbContext, new FixedClock(now));
            var job = JobRecord.Create("conversion", "{}", now);
            job.AddEvent(JobEventLevel.Information, "job.queued", "Saved.", now);
            await repository.EnqueueAsync(job, CancellationToken.None);
            jobId = job.Id;
        }

        await using (var dbContext = new JobsDbContext(options))
        {
            var repository = new EfJobRepository(dbContext, new FixedClock(now.AddSeconds(1)));
            var restoredJob = await repository.GetByIdAsync(jobId, CancellationToken.None);

            Assert.NotNull(restoredJob);
            Assert.Equal(JobStatus.Queued, restoredJob.Status);
            Assert.Equal("job.queued", Assert.Single(restoredJob.Events).Code);

            Assert.True(await repository.TryBeginAttemptAsync(jobId, CancellationToken.None));
            var claimedJob = await repository.GetForProcessingAsync(jobId, CancellationToken.None);
            Assert.NotNull(claimedJob);
            claimedJob.AddEvent(JobEventLevel.Information, "job.started", "Started.", now.AddSeconds(1));
            await repository.UpdateAsync(claimedJob, CancellationToken.None);
        }

        await using (var recoveryContext = new JobsDbContext(options))
        {
            var repository = new EfJobRepository(recoveryContext, new FixedClock(now.AddMinutes(31)));
            await repository.RequeueInterruptedAsync(CancellationToken.None);
        }

        await using (var readContext = new JobsDbContext(options))
        {
            var recoveredJob = await new EfJobRepository(readContext, new FixedClock(now.AddMinutes(31)))
                .GetByIdAsync(jobId, CancellationToken.None);

            Assert.NotNull(recoveredJob);
            Assert.Equal(JobStatus.Queued, recoveredJob.Status);
            Assert.Null(recoveredJob.AttemptStartedAt);
            Assert.Equal(2, recoveredJob.AttemptCount);
            Assert.Equal("job.recovered", recoveredJob.Events.Last().Code);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
