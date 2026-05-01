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
                    request.FlowId,
                    request.EstimatedPoints,
                    request.EstimatedCompletionDate,
                    request.AssigneeId);

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Created(string.Empty, result.Value),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("CreateWorkItem")
            .WithTags(EndpointTags.WorkItems)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record CreateWorkItemRequest(
        string Title,
        string? Description,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        WorkItemType Type,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        Priority Priority,
        Guid ProjectId,
        Guid FlowId,
        int? EstimatedPoints,
        DateOnly? EstimatedCompletionDate,
        Guid? AssigneeId);
}
