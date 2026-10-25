using AbGen.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AbGen.Api.Data.Repositories;

public sealed class EfProjectRepository(ProjectDbContext dbContext) : IProjectRepository
{
    public async Task<Project> AddAsync(Project project, CancellationToken cancellationToken)
    {
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Projects.AsNoTracking()
            .OrderByDescending(project => project.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Projects.AsNoTracking()
            .SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
}
