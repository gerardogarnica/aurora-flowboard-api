using Aurora.Flowboard.Application.Projects.ChangeStatus;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class ChangeProjectStatus : IBaseEndpoint
{
    // One PATCH route per target status; the segment and operation name are the public
    // contract, so they stay exactly as they were when each route had its own class.
    private static readonly (string Segment, ProjectStatus Status, string Name)[] Transitions =
    [
        ("activate", ProjectStatus.Active, "ActivateProject"),
        ("maintenance", ProjectStatus.Maintenance, "MaintenanceProject"),
        ("complete", ProjectStatus.Completed, "CompleteProject"),
        ("archive", ProjectStatus.Archived, "ArchiveProject"),
    ];

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        foreach ((string segment, ProjectStatus status, string name) in Transitions)
        {
            app.MapPatch(
                $"projects/{{id:guid}}/{segment}",
                async (
                    Guid id,
                    ICommandHandler<ChangeProjectStatusCommand> handler,
                    CancellationToken cancellationToken) =>
                {
                    var command = new ChangeProjectStatusCommand(id, status);

                    Result result = await handler.Handle(command, cancellationToken);

                    return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
                })
                .RequireAuthorization()
                .WithName(name)
                .WithTags(EndpointTags.Projects)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesStandardProblems(
                    StatusCodes.Status400BadRequest,
                    StatusCodes.Status403Forbidden,
                    StatusCodes.Status404NotFound,
                    StatusCodes.Status409Conflict);
        }
    }
}
