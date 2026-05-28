using Aurora.Flowboard.Application.Flows.AddState;
using Aurora.Flowboard.Domain.Flows;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Api.Endpoints.Flows;

public sealed class AddFlowState : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "flows/{id:guid}/states",
            async (
                Guid id,
                [FromBody] AddFlowStateRequest request,
                ICommandHandler<AddFlowStateCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new AddFlowStateCommand(
                    id,
                    request.Name,
                    request.Category,
                    request.AllowedRoles);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("AddFlowState")
            .WithTags(EndpointTags.Flows)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record AddFlowStateRequest(
        string Name,
        FlowStateCategory Category,
        IReadOnlyCollection<ProjectRole> AllowedRoles);
}
