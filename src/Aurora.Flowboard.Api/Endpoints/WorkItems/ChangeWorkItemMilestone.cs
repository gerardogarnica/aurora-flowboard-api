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

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("ChangeWorkItemMilestone")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record ChangeWorkItemMilestoneRequest(Guid? MilestoneId);
}
