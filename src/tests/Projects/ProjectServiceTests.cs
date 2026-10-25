using AbGen.Api.Data.Entities;
using AbGen.Api.Data.Repositories;
using AbGen.Api.Services;

namespace AbGen.Tests.Projects;

public sealed class ProjectServiceTests
{
    [Fact]
    public async Task Create_persists_and_returns_project_metadata()
    {
        var repository = new InMemoryProjectRepository();
        var service = new ProjectService(repository, new FixedTimeProvider());

        var response = await service.CreateAsync(new CreateProjectRequest { Name = "  New project  " }, CancellationToken.None);

        var stored = Assert.Single(repository.Projects);
        Assert.Equal(stored.Id, response.Id);
        Assert.Equal("New project", response.Name);
        Assert.Equal(FixedTimeProvider.Now, response.CreatedAt);
        Assert.Equal(FixedTimeProvider.Now, response.UpdatedAt);
    }

    [Fact]
    public async Task Project_list_is_mapped_from_persisted_records()
    {
        var repository = new InMemoryProjectRepository();
        repository.Projects.Add(Project.Create("A project", FixedTimeProvider.Now));
        var service = new ProjectService(repository, new FixedTimeProvider());

        var projects = await service.GetAllAsync(CancellationToken.None);

        Assert.Equal("A project", Assert.Single(projects).Name);
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        public List<Project> Projects { get; } = [];

        public Task<Project> AddAsync(Project project, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Projects.Add(project);
            return Task.FromResult(project);
        }

        public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<Project> projects = Projects.ToArray();
            return Task.FromResult(projects);
        }

        public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Projects.SingleOrDefault(project => project.Id == id));
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
