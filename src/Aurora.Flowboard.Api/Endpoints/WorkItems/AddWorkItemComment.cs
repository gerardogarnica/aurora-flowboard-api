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

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("AddWorkItemComment")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record AddWorkItemCommentRequest(string Content);
}
