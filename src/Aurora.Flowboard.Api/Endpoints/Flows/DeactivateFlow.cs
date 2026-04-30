using Aurora.Flowboard.Application.Flows.Deactivate;

namespace Aurora.Flowboard.Api.Endpoints.Flows;

public sealed class DeactivateFlow : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "flows/{id:guid}/deactivate",
            async (
                Guid id,
                ICommandHandler<DeactivateFlowCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new DeactivateFlowCommand(id);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("DeactivateFlow")
            .WithTags(EndpointTags.Flows)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
