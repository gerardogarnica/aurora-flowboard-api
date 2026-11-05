using Aurora.Flowboard.Application.WorkItems.UpdateType;
using Aurora.Flowboard.Domain.WorkItems;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemType : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/type",
            async (
                Guid id,
                [FromBody] UpdateWorkItemTypeRequest request,
                ICommandHandler<UpdateWorkItemTypeCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemTypeCommand(id, request.Type);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemType")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record UpdateWorkItemTypeRequest(WorkItemType Type);
}
