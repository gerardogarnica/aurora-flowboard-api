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

                return result.Match(ApiResponses.Accepted, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("LogWorkItemTime")
            .WithTags(EndpointTags.WorkItems)
            .Produces(StatusCodes.Status202Accepted)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound,
                StatusCodes.Status409Conflict);
    }

    internal sealed record LogWorkItemTimeRequest(
        decimal Hours,
        string? Description,
        DateTime LoggedOnUtc);
}
