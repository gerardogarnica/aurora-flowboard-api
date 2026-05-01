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

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("RemoveWorkItemTag")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
