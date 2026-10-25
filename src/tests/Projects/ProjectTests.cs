using AbGen.Api.Data.Entities;

namespace AbGen.Tests.Projects;

public sealed class ProjectTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_trims_name_and_records_creation_and_update_times()
    {
        var project = Project.Create("  The Wind in the Willows  ", CreatedAt);

        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal("The Wind in the Willows", project.Name);
        Assert.Equal(CreatedAt, project.CreatedAt);
        Assert.Equal(CreatedAt, project.UpdatedAt);
    }

    [Fact]
    public void Create_rejects_blank_or_too_long_names()
    {
        Assert.Throws<ArgumentException>(() => Project.Create("   ", CreatedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() => Project.Create(new string('x', Project.MaximumNameLength + 1), CreatedAt));
    }
}
