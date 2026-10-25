using AbGen.Api.Data.Entities;

namespace AbGen.Api.Data.Repositories;

public interface IProjectRepository
{
    Task<Project> AddAsync(Project project, CancellationToken cancellationToken);
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
