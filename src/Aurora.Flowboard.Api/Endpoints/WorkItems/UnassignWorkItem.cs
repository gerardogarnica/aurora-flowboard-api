using Aurora.Flowboard.Application.WorkItems.Unassign;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UnassignWorkItem : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/unassign",
            async (
                Guid id,
                ICommandHandler<UnassignWorkItemCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UnassignWorkItemCommand(id);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UnassignWorkItem")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
