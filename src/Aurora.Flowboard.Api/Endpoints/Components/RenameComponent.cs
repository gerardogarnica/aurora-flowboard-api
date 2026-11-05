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

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("RenameComponent")
            .WithTags(EndpointTags.Components)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record RenameComponentRequest(string Name);
}
