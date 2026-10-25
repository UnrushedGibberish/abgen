using AbGen.Api.Data.Entities;
using AbGen.Api.Data.Repositories;

namespace AbGen.Api.Services;

public sealed class ProjectService(
    IProjectRepository repository,
    TimeProvider timeProvider) : IProjectService
{
    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = Project.Create(request.Name, timeProvider.GetUtcNow());
        await repository.AddAsync(project, cancellationToken);
        return ToResponse(project);
    }

    public async Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var projects = await repository.GetAllAsync(cancellationToken);
        return projects.Select(ToResponse).ToArray();
    }

    public async Task<ProjectResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(id, cancellationToken);
        return project is null ? null : ToResponse(project);
    }

    private static ProjectResponse ToResponse(Project project) => new(
        project.Id,
        project.Name,
        project.CreatedAt,
        project.UpdatedAt);
}
