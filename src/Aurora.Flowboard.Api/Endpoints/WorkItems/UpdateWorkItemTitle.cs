using Aurora.Flowboard.Application.WorkItems.UpdateTitle;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItemTitle : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/title",
            async (
                Guid id,
                [FromBody] UpdateWorkItemTitleRequest request,
                ICommandHandler<UpdateWorkItemTitleCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemTitleCommand(id, request.Title);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItemTitle")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record UpdateWorkItemTitleRequest(string Title);
}
