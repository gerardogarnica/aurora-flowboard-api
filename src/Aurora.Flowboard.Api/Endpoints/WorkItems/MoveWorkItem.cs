using Aurora.Flowboard.Application.WorkItems.Move;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class MoveWorkItem : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/move",
            async (
                Guid id,
                [FromBody] MoveWorkItemRequest request,
                ICommandHandler<MoveWorkItemCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new MoveWorkItemCommand(id, request.ToStateId, request.Reason);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("MoveWorkItem")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record MoveWorkItemRequest(Guid ToStateId, string? Reason);
}
