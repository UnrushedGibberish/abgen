namespace AbGen.Api.Services;

public interface IProjectService
{
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<ProjectResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
