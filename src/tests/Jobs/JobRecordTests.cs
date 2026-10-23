using AbGen.Api.Domain.Jobs;

namespace AbGen.Tests.Jobs;

public sealed class JobRecordTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_queued_with_no_progress_and_sequence_zero_events()
    {
        var job = JobRecord.Create("placeholder", "{}", CreatedAt);
        job.AddEvent(JobEventLevel.Information, "job.queued", "Saved.", CreatedAt);

        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.Equal(0, job.ProgressPercent);
        Assert.Null(job.CompletedAt);
        Assert.NotEqual(Guid.Empty, job.Id);
        var firstEvent = Assert.Single(job.Events);
        Assert.Equal(0, firstEvent.SequenceNumber);
    }

    [Fact]
    public void Successful_job_persists_progress_and_ordered_user_events()
    {
        var job = JobRecord.Create("placeholder", "{}", CreatedAt);
        job.AddEvent(JobEventLevel.Information, "job.queued", "Saved.", CreatedAt);
        job.MarkRunning(CreatedAt.AddSeconds(1));
        job.AddEvent(JobEventLevel.Information, "job.started", "Started.", CreatedAt.AddSeconds(1));
        job.ReportProgress(50, "Halfway", CreatedAt.AddSeconds(2));
        job.AddEvent(JobEventLevel.Information, "job.progress", "Halfway.", CreatedAt.AddSeconds(2));
        job.MarkSucceeded(CreatedAt.AddSeconds(3));
        job.AddEvent(JobEventLevel.Information, "job.succeeded", "Done.", CreatedAt.AddSeconds(3));

        Assert.Equal(JobStatus.Succeeded, job.Status);
        Assert.Equal(100, job.ProgressPercent);
        Assert.Equal(CreatedAt.AddSeconds(3), job.CompletedAt);
        Assert.Collection(job.Events,
            item => Assert.Equal(0, item.SequenceNumber),
            item => Assert.Equal(1, item.SequenceNumber),
            item => Assert.Equal(2, item.SequenceNumber),
            item => Assert.Equal(3, item.SequenceNumber));
    }

    [Fact]
    public void Interrupted_running_job_can_be_requeued_and_retried()
    {
        var job = JobRecord.Create("placeholder", "{}", CreatedAt);
        job.MarkRunning(CreatedAt.AddSeconds(1));
        job.ReportProgress(50, "Halfway", CreatedAt.AddSeconds(2));
        job.RequeueAfterInterruptedAttempt(CreatedAt.AddSeconds(3));
        job.AddEvent(JobEventLevel.Warning, "job.recovered", "Retrying.", CreatedAt.AddSeconds(3));

        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.Equal(50, job.ProgressPercent);
        Assert.Equal(CreatedAt.AddSeconds(1), job.StartedAt);
        Assert.Null(job.CompletedAt);
        Assert.Equal(2, job.AttemptCount);
        Assert.Equal("job.recovered", job.Events.Single().Code);
    }

    [Fact]
    public void Invalid_state_transitions_and_progress_are_rejected()
    {
        var job = JobRecord.Create("placeholder", "{}", CreatedAt);

        Assert.Throws<InvalidOperationException>(() => job.ReportProgress(10, "No", CreatedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() => job.AddEvent(JobEventLevel.Information, "too-long", new string('x', 2001), CreatedAt));

        job.MarkRunning(CreatedAt.AddSeconds(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => job.ReportProgress(101, "Too high", CreatedAt.AddSeconds(2)));
        Assert.Throws<InvalidOperationException>(() => job.MarkRunning(CreatedAt.AddSeconds(2)));
    }
}
