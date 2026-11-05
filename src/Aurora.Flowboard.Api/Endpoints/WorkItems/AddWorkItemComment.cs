using Aurora.Flowboard.Application.WorkItems.AddComment;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class AddWorkItemComment : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "work-items/{id:guid}/comments",
            async (
                Guid id,
                [FromBody] AddWorkItemCommentRequest request,
                ICommandHandler<AddWorkItemCommentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new AddWorkItemCommentCommand(id, request.Content);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("AddWorkItemComment")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record AddWorkItemCommentRequest(string Content);
}
