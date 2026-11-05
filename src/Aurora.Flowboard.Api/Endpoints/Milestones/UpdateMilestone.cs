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
                    request.Color,
                    request.TargetStartDate,
                    request.TargetEndDate);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateMilestone")
            .WithTags(EndpointTags.Milestones)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record UpdateMilestoneRequest(
        string Name,
        string? Description,
        string Color,
        DateOnly? TargetStartDate,
        DateOnly? TargetEndDate);
}
