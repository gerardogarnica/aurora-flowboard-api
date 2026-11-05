using Aurora.Flowboard.Application.WorkItems.Create;
using Aurora.Flowboard.Domain.WorkItems;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class CreateWorkItem : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "work-items",
            async (
                [FromBody] CreateWorkItemRequest request,
                ICommandHandler<CreateWorkItemCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateWorkItemCommand(
                    request.Title,
                    request.Description,
                    request.Type,
                    request.Priority,
                    request.ProjectId,
                    request.EstimatedPoints,
                    request.EstimatedCompletionDate,
                    request.AssigneeId,
                    request.MilestoneId,
                    request.ComponentId);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Created(string.Empty, result.Value),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("CreateWorkItem")
            .WithTags(EndpointTags.WorkItems)
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status409Conflict);
    }

    internal sealed record CreateWorkItemRequest(
        string Title,
        string? Description,
        WorkItemType Type,
        Priority Priority,
        Guid ProjectId,
        int? EstimatedPoints,
        DateOnly? EstimatedCompletionDate,
        Guid? AssigneeId,
        Guid? MilestoneId,
        Guid? ComponentId);
}
