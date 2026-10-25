namespace AbGen.Api.Data.Entities;

public sealed class Project
{
    public const int MaximumNameLength = 200;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Project() { }

    public static Project Create(string name, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalizedName = name.Trim();
        if (normalizedName.Length > MaximumNameLength)
        {
            throw new ArgumentOutOfRangeException(nameof(name), $"Project name must be {MaximumNameLength} characters or fewer.");
        }

        return new Project
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
