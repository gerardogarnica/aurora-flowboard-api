using Aurora.Flowboard.Application.Milestones.ChangeStatus;
using Aurora.Flowboard.Domain.Milestones;

namespace Aurora.Flowboard.Api.Endpoints.Milestones;

public sealed class ChangeMilestoneStatus : IBaseEndpoint
{
    // One PATCH route per target status; the segment and operation name are the public
    // contract, so they stay exactly as they were when each route had its own class.
    private static readonly (string Segment, MilestoneStatus Status, string Name)[] Transitions =
    [
        ("activate", MilestoneStatus.Active, "ActivateMilestone"),
        ("hold", MilestoneStatus.OnHold, "HoldMilestone"),
        ("complete", MilestoneStatus.Completed, "CompleteMilestone"),
        ("archive", MilestoneStatus.Archived, "ArchiveMilestone"),
    ];

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        foreach ((string segment, MilestoneStatus status, string name) in Transitions)
        {
            app.MapPatch(
                $"milestones/{{id:guid}}/{segment}",
                async (
                    Guid id,
                    ICommandHandler<ChangeMilestoneStatusCommand> handler,
                    CancellationToken cancellationToken) =>
                {
                    var command = new ChangeMilestoneStatusCommand(id, status);

                    Result result = await handler.Handle(command, cancellationToken);

                    return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
                })
                .RequireAuthorization()
                .WithName(name)
                .WithTags(EndpointTags.Milestones)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesStandardProblems(
                    StatusCodes.Status400BadRequest,
                    StatusCodes.Status403Forbidden,
                    StatusCodes.Status404NotFound,
                    StatusCodes.Status409Conflict);
        }
    }
}
