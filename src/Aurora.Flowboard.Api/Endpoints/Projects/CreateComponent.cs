using Aurora.Flowboard.Application.Components.Create;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class CreateComponent : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "projects/{id:guid}/components",
            async (
                Guid id,
                [FromBody] CreateComponentRequest request,
                ICommandHandler<CreateComponentCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateComponentCommand(id, request.Name);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Created(string.Empty, result.Value),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("CreateComponent")
            .WithTags(EndpointTags.Projects)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record CreateComponentRequest(string Name);
}
