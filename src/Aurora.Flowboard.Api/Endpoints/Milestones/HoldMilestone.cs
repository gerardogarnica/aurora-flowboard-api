using Aurora.Flowboard.Application.Milestones.ChangeStatus;
using Aurora.Flowboard.Domain.Milestones;

namespace Aurora.Flowboard.Api.Endpoints.Milestones;

public sealed class HoldMilestone : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "milestones/{id:guid}/hold",
            async (
                Guid id,
                ICommandHandler<ChangeMilestoneStatusCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ChangeMilestoneStatusCommand(id, MilestoneStatus.OnHold);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("HoldMilestone")
            .WithTags(EndpointTags.Milestones)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
