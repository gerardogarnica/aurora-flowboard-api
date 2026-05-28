using Aurora.Flowboard.Application.Flows.RemoveTransitionRole;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Api.Endpoints.Flows;

public sealed class RemoveFlowTransitionRole : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "flows/{id:guid}/transitions/{transitionId:guid}/roles",
            async (
                Guid id,
                Guid transitionId,
                [FromBody] RemoveFlowTransitionRoleRequest request,
                ICommandHandler<RemoveFlowTransitionRoleCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveFlowTransitionRoleCommand(id, transitionId, request.Role);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("RemoveFlowTransitionRole")
            .WithTags(EndpointTags.Flows)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record RemoveFlowTransitionRoleRequest(
        ProjectRole Role);
}
