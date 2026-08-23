using Aurora.Flowboard.Application.WorkItems.UpdatePriority;
using Aurora.Flowboard.Domain.WorkItems;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemPriority : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/priority",
            async (
                Guid id,
                [FromBody] UpdateWorkItemPriorityRequest request,
                ICommandHandler<UpdateWorkItemPriorityCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemPriorityCommand(id, request.Priority);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemPriority")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateWorkItemPriorityRequest(Priority Priority);
}
