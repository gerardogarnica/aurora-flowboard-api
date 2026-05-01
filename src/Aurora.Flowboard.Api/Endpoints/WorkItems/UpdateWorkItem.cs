using Aurora.Flowboard.Application.WorkItems.Update;
using Aurora.Flowboard.Domain.WorkItems;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class UpdateWorkItem : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "work-items/{id:guid}",
            async (
                Guid id,
                [FromBody] UpdateWorkItemRequest request,
                ICommandHandler<UpdateWorkItemCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateWorkItemCommand(
                    id,
                    request.Title,
                    request.Description,
                    request.Priority,
                    request.EstimatedPoints,
                    request.EstimatedCompletionDate);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("UpdateWorkItem")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record UpdateWorkItemRequest(
        string Title,
        string? Description,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        Priority Priority,
        int? EstimatedPoints,
        DateOnly? EstimatedCompletionDate);
}
