using Aurora.Flowboard.Application.Milestones.Create;

namespace Aurora.Flowboard.Api.Endpoints.Milestones;

public sealed class CreateMilestone : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "projects/{id:guid}/milestones",
            async (
                Guid id,
                [FromBody] CreateMilestoneRequest request,
                ICommandHandler<CreateMilestoneCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateMilestoneCommand(
                    id,
                    request.Name,
                    request.Description,
                    request.TargetStartDate,
                    request.TargetEndDate);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Created(string.Empty, result.Value),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("CreateMilestone")
            .WithTags(EndpointTags.Milestones)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record CreateMilestoneRequest(
        string Name,
        string? Description,
        DateOnly? TargetStartDate,
        DateOnly? TargetEndDate);
}
