using Aurora.Flowboard.Application.WorkItems.Unassign;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UnassignWorkItem : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/unassign",
            async (
                Guid id,
                ICommandHandler<UnassignWorkItemCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UnassignWorkItemCommand(id);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UnassignWorkItem")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }
}
