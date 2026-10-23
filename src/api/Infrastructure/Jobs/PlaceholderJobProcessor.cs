using AbGen.Api.Domain.Jobs;
using AbGen.Api.Application.Jobs;

namespace AbGen.Api.Infrastructure.Jobs;

public sealed class PlaceholderJobProcessor(ISystemClock clock) : IJobProcessor
{
    public string JobType => "placeholder";

    public async Task ProcessAsync(
        JobRecord job,
        Func<JobProgressUpdate, CancellationToken, Task> reportProgress,
        CancellationToken cancellationToken)
    {
        job.AddEvent(JobEventLevel.Information, "placeholder.started", "The durable placeholder job started processing.", clock.UtcNow);
        await reportProgress(
            new JobProgressUpdate(25, "Checking the job pipeline", JobEventLevel.Information,
                "placeholder.stage.started", "The hosted service picked up this job."),
            cancellationToken);

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        await reportProgress(
            new JobProgressUpdate(75, "Finishing placeholder work", JobEventLevel.Information,
                "placeholder.stage.progress", "Placeholder work reached the second stage."),
            cancellationToken);

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        job.MarkSucceeded(clock.UtcNow);
        job.AddEvent(JobEventLevel.Information, "job.succeeded", "The placeholder job completed successfully.", clock.UtcNow);
    }
}
