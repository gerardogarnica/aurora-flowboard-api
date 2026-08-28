using Aurora.Flowboard.Application.Components.Rename;

namespace Aurora.Flowboard.Api.Endpoints.Components;

public sealed class RenameComponent : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "components/{id:guid}",
            async (
                Guid id,
                [FromBody] RenameComponentRequest request,
                ICommandHandler<RenameComponentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RenameComponentCommand(id, request.Name);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("RenameComponent")
            .WithTags(EndpointTags.Components)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record RenameComponentRequest(string Name);
}
