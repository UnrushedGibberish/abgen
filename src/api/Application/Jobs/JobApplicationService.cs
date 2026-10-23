using AbGen.Api.Domain.Jobs;
using AbGen.Api.Infrastructure.Jobs;

namespace AbGen.Api.Application.Jobs;

public sealed class JobApplicationService(IJobRepository repository, ISystemClock clock)
{
    private const string PlaceholderJobType = "placeholder";

    public async Task<JobRecord> EnqueuePlaceholderAsync(CancellationToken cancellationToken)
    {
        var job = JobRecord.Create(PlaceholderJobType, "{}", clock.UtcNow);
        job.AddEvent(JobEventLevel.Information, "job.queued", "Job accepted and saved to durable storage.", clock.UtcNow);
        return await repository.EnqueueAsync(job, cancellationToken);
    }

    public Task<IReadOnlyList<JobRecord>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<JobRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);
}
