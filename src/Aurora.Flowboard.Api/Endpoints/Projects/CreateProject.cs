using Aurora.Flowboard.Application.Projects.Create;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class CreateProject : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "projects",
            async (
                [FromBody] CreateProjectRequest request,
                ICommandHandler<CreateProjectCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateProjectCommand(
                    request.Name,
                    request.Description,
                    request.Prefix,
                    request.Kind,
                    request.Color);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Created(string.Empty, result.Value),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("CreateProject")
            .WithTags(EndpointTags.Projects)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record CreateProjectRequest(
        string Name,
        string? Description,
        string Prefix,
        ProjectKind Kind,
        string Color);
}
