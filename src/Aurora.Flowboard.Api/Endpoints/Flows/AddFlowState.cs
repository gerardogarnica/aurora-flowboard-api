using Aurora.Flowboard.Application.Projects.AddFlowState;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Api.Endpoints.Flows;

public sealed class AddFlowState : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "projects/{id:guid}/flow/states",
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
                    request.Color,
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
        string Color,
        IReadOnlyCollection<ProjectRole> AllowedRoles);
}
