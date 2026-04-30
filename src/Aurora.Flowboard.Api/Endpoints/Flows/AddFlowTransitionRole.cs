using Aurora.Flowboard.Application.Flows.AddTransitionRole;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Api.Endpoints.Flows;

public sealed class AddFlowTransitionRole : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "flows/{id:guid}/transitions/{transitionId:guid}/roles",
            async (
                Guid id,
                Guid transitionId,
                [FromBody] AddFlowTransitionRoleRequest request,
                ICommandHandler<AddFlowTransitionRoleCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new AddFlowTransitionRoleCommand(id, transitionId, request.Role);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("AddFlowTransitionRole")
            .WithTags(EndpointTags.Flows)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record AddFlowTransitionRoleRequest(
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        ProjectRole Role);
}
