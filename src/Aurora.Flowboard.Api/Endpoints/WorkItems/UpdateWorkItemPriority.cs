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

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemPriority")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record UpdateWorkItemPriorityRequest(Priority Priority);
}
