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

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("MoveWorkItem")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record MoveWorkItemRequest(Guid ToStateId, string? Reason);
}
