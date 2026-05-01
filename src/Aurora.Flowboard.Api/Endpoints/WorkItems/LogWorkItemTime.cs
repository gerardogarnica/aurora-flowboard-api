using Aurora.Flowboard.Application.WorkItems.LogTime;

namespace Aurora.Flowboard.Api.Endpoints.WorkItems;

public sealed class LogWorkItemTime : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "work-items/{id:guid}/time-entries",
            async (
                Guid id,
                [FromBody] LogWorkItemTimeRequest request,
                ICommandHandler<LogWorkItemTimeCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new LogWorkItemTimeCommand(
                    id,
                    request.Hours,
                    request.Description,
                    request.LoggedOnUtc);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(
                    () => Results.Accepted(string.Empty),
                    ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("LogWorkItemTime")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }

    internal sealed record LogWorkItemTimeRequest(
        decimal Hours,
        string? Description,
        DateTime LoggedOnUtc);
}
