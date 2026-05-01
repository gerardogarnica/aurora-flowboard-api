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

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemComment")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateWorkItemCommentRequest(string Content);
}
