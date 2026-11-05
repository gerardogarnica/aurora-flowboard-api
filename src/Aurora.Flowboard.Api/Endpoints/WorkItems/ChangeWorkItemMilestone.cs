using Aurora.Flowboard.Application.WorkItems.ChangeMilestone;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class ChangeWorkItemMilestone : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "work-items/{id:guid}/milestone",
            async (
                Guid id,
                [FromBody] ChangeWorkItemMilestoneRequest request,
                ICommandHandler<ChangeWorkItemMilestoneCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ChangeWorkItemMilestoneCommand(id, request.MilestoneId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("ChangeWorkItemMilestone")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record ChangeWorkItemMilestoneRequest(Guid? MilestoneId);
}
