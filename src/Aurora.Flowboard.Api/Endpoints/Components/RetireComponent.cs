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

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("RetireComponent")
            .WithTags(EndpointTags.Components)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
