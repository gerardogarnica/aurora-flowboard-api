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

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("AddWorkItemTag")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record AddWorkItemTagRequest(string Name);
}
