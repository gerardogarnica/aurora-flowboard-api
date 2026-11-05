using Aurora.Flowboard.Application.WorkItems.UpdateComment;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemComment : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "work-items/{id:guid}/comments/{commentId:guid}",
            async (
                Guid id,
                Guid commentId,
                [FromBody] UpdateWorkItemCommentRequest request,
                ICommandHandler<UpdateWorkItemCommentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemCommentCommand(id, commentId, request.Content);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemComment")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record UpdateWorkItemCommentRequest(string Content);
}
