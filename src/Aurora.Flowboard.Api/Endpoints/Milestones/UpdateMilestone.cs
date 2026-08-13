using Aurora.Flowboard.Application.Milestones.Update;

namespace Aurora.Flowboard.Api.Endpoints.Milestones;

public sealed class UpdateMilestone : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "milestones/{id:guid}",
            async (
                Guid id,
                [FromBody] UpdateMilestoneRequest request,
                ICommandHandler<UpdateMilestoneCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateMilestoneCommand(
                    id,
                    request.Name,
                    request.Description,
                    request.TargetStartDate,
                    request.TargetEndDate);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateMilestone")
            .WithTags(EndpointTags.Milestones)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateMilestoneRequest(
        string Name,
        string? Description,
        DateOnly? TargetStartDate,
        DateOnly? TargetEndDate);
}
