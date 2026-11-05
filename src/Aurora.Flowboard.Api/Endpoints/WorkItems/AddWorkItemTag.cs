using Aurora.Flowboard.Application.WorkItems.AddTag;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class AddWorkItemTag : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "work-items/{id:guid}/tags",
            async (
                Guid id,
                [FromBody] AddWorkItemTagRequest request,
                ICommandHandler<AddWorkItemTagCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new AddWorkItemTagCommand(id, request.Name);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("AddWorkItemTag")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record AddWorkItemTagRequest(string Name);
}
