using AbGen.Api.Domain.Jobs;

namespace AbGen.Api.Application.Jobs;

public interface IJobRepository
{
    Task<JobRecord> EnqueueAsync(JobRecord job, CancellationToken cancellationToken);
    Task<IReadOnlyList<JobRecord>> GetAllAsync(CancellationToken cancellationToken);
    Task<JobRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<JobRecord?> GetForProcessingAsync(Guid id, CancellationToken cancellationToken);
    Task<JobRecord?> GetNextQueuedAsync(CancellationToken cancellationToken);
    Task RequeueInterruptedAsync(CancellationToken cancellationToken);
    Task<bool> TryBeginAttemptAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(JobRecord job, CancellationToken cancellationToken);
}
