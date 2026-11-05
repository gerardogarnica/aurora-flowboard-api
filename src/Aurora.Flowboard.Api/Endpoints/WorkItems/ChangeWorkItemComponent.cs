using Aurora.Flowboard.Application.WorkItems.ChangeComponent;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class ChangeWorkItemComponent : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/component",
            async (
                Guid id,
                [FromBody] ChangeWorkItemComponentRequest request,
                ICommandHandler<ChangeWorkItemComponentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ChangeWorkItemComponentCommand(id, request.ComponentId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("ChangeWorkItemComponent")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record ChangeWorkItemComponentRequest(Guid? ComponentId);
}
