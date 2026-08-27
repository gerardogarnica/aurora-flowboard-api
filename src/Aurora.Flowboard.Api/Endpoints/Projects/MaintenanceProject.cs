using Aurora.Flowboard.Application.Projects.ChangeStatus;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class MaintenanceProject : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "projects/{id:guid}/maintenance",
            async (
                Guid id,
                ICommandHandler<ChangeProjectStatusCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ChangeProjectStatusCommand(id, ProjectStatus.Maintenance);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("MaintenanceProject")
            .WithTags(EndpointTags.Projects)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
