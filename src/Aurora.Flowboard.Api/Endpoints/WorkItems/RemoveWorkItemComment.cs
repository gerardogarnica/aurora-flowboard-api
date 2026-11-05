using Aurora.Flowboard.Application.WorkItems.RemoveComment;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class RemoveWorkItemComment : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "work-items/{id:guid}/comments/{commentId:guid}",
            async (
                Guid id,
                Guid commentId,
                ICommandHandler<RemoveWorkItemCommentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RemoveWorkItemCommentCommand(id, commentId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("RemoveWorkItemComment")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }
}
