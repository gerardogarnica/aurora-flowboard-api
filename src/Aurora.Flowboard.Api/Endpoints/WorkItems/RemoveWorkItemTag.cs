using Aurora.Flowboard.Application.WorkItems.RemoveTag;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class RemoveWorkItemTag : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "work-items/{id:guid}/tags/{tagId:guid}",
            async (
                Guid id,
                Guid tagId,
                ICommandHandler<RemoveWorkItemTagCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveWorkItemTagCommand(id, tagId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("RemoveWorkItemTag")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }
}
