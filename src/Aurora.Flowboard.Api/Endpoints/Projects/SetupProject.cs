using Aurora.Flowboard.Application.Projects.Setup;
using Aurora.Flowboard.Domain.Flows;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class SetupProject : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "projects",
            async (
                [FromBody] SetupProjectRequest request,
                ICommandHandler<SetupProjectCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new SetupProjectCommand(
                    request.Name,
                    request.Description,
                    request.Code,
                    request.Color,
                    request.EstimatedCompletionDate,
                    new SetupProjectFlowDto(
                        request.Flow.Name,
                        request.Flow.Description,
                        [.. request.Flow.States.Select(s => new SetupProjectFlowStateDto(s.Name, s.Category, s.Roles))]));

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Created(string.Empty, result.Value),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("SetupProject")
            .WithTags(EndpointTags.Projects)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record SetupProjectRequest(
        string Name,
        string? Description,
        string Code,
        string Color,
        DateOnly? EstimatedCompletionDate,
        SetupProjectFlowRequest Flow);

    internal sealed record SetupProjectFlowRequest(
        string Name,
        string? Description,
        IReadOnlyCollection<SetupProjectFlowStateRequest> States);

    internal sealed record SetupProjectFlowStateRequest(
        string Name,
        FlowStateCategory Category,
        IReadOnlyCollection<ProjectRole> Roles);
}
