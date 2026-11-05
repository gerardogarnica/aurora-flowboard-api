using Aurora.Flowboard.Application.WorkItems.Assign;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class AssignWorkItem : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/assign",
            async (
                Guid id,
                [FromBody] AssignWorkItemRequest request,
                ICommandHandler<AssignWorkItemCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new AssignWorkItemCommand(id, request.AssigneeId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("AssignWorkItem")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record AssignWorkItemRequest(Guid AssigneeId);
}
