using AbGen.Api.Domain.Jobs;
using AbGen.Api.Infrastructure.Jobs;

namespace AbGen.Api.Application.Jobs;

public sealed class JobApplicationService(IJobRepository repository)
{
    public Task<IReadOnlyList<JobRecord>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<JobRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);
}
