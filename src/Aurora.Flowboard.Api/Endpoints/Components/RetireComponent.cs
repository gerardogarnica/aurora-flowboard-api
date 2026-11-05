using Aurora.Flowboard.Application.Components.Retire;

namespace Aurora.Flowboard.Api.Endpoints.Components;

public sealed class RetireComponent : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "components/{id:guid}/retire",
            async (
                Guid id,
                ICommandHandler<RetireComponentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RetireComponentCommand(id);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("RetireComponent")
            .WithTags(EndpointTags.Components)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }
}
