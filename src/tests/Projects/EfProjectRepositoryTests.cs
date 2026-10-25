using AbGen.Api.Data;
using AbGen.Api.Data.Entities;
using AbGen.Api.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AbGen.Tests.Projects;

public sealed class EfProjectRepositoryTests
{
    [Fact]
    public async Task Project_metadata_roundtrips_and_lists_newest_first()
    {
        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var createdAt = new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
        var olderId = Guid.Empty;
        var newerId = Guid.Empty;

        await using (var dbContext = new ProjectDbContext(options))
        {
            var repository = new EfProjectRepository(dbContext);
            var older = await repository.AddAsync(Project.Create("Older", createdAt), CancellationToken.None);
            var newer = await repository.AddAsync(Project.Create("Newer", createdAt.AddMinutes(1)), CancellationToken.None);
            olderId = older.Id;
            newerId = newer.Id;
        }

        await using var readContext = new ProjectDbContext(options);
        var readRepository = new EfProjectRepository(readContext);
        var projects = await readRepository.GetAllAsync(CancellationToken.None);
        var restored = await readRepository.GetByIdAsync(olderId, CancellationToken.None);

        Assert.Collection(projects,
            project => Assert.Equal(newerId, project.Id),
            project => Assert.Equal(olderId, project.Id));
        Assert.NotNull(restored);
        Assert.Equal("Older", restored.Name);
        Assert.Equal(createdAt, restored.CreatedAt);
        Assert.Null(await readRepository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
