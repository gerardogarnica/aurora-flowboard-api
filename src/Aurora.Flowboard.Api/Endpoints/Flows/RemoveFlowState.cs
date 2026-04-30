using Aurora.Flowboard.Application.Flows.RemoveState;

namespace Aurora.Flowboard.Api.Endpoints.Flows;

public sealed class RemoveFlowState : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "flows/{id:guid}/states/{stateId:guid}",
            async (
                Guid id,
                Guid stateId,
                ICommandHandler<RemoveFlowStateCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveFlowStateCommand(id, stateId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("RemoveFlowState")
            .WithTags(EndpointTags.Flows)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
