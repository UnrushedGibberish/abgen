using System.ComponentModel.DataAnnotations;
using AbGen.Api.Data.Entities;

namespace AbGen.Api.Services;

/// <summary>Payload for creating a project.</summary>
public sealed record CreateProjectRequest
{
    /// <summary>The project name.</summary>
    [Required, StringLength(Project.MaximumNameLength)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Project name cannot be empty.")]
    public required string Name { get; init; }
}

/// <summary>Project metadata returned by the API.</summary>
public sealed record ProjectResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
