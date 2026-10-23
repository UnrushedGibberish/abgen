using AbGen.Api.Domain.Jobs;

namespace AbGen.Api.Application.Jobs;

public sealed record JobProgressUpdate(
    int ProgressPercent,
    string CurrentStep,
    JobEventLevel Level,
    string Code,
    string Message);

public interface IJobProcessor
{
    string JobType { get; }
    Task ProcessAsync(
        JobRecord job,
        Func<JobProgressUpdate, CancellationToken, Task> reportProgress,
        CancellationToken cancellationToken);
}

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
