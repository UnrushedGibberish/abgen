using AbGen.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AbGen.Api.Controllers;

public static class ProjectEndpoints
{
    public static WebApplication MapProjectEndpoints(this WebApplication app)
    {
        var projects = app.MapGroup("/api/projects").WithTags("Projects");

        projects.MapGet("", GetAllAsync)
            .WithName("GetProjects")
            .WithSummary("List projects")
            .WithDescription("Returns project metadata ordered from newest to oldest.")
            .Produces<IReadOnlyList<ProjectResponse>>(StatusCodes.Status200OK);

        projects.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetProjectById")
            .WithSummary("Get a project by ID")
            .WithDescription("Returns project metadata for the specified project.")
            .Produces<ProjectResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        projects.MapPost("", CreateAsync)
            .WithName("CreateProject")
            .WithSummary("Create a project")
            .WithDescription("Creates an empty audiobook project.")
            .Produces<ProjectResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProjectResponse>>> GetAllAsync(
        IProjectService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAllAsync(cancellationToken));

    private static async Task<Results<Ok<ProjectResponse>, NotFound>> GetByIdAsync(
        Guid id,
        IProjectService service,
        CancellationToken cancellationToken)
    {
        var project = await service.GetByIdAsync(id, cancellationToken);
        return project is null ? TypedResults.NotFound() : TypedResults.Ok(project);
    }

    private static async Task<Created<ProjectResponse>> CreateAsync(
        CreateProjectRequest request,
        IProjectService service,
        CancellationToken cancellationToken)
    {
        var project = await service.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/projects/{project.Id}", project);
    }
}
