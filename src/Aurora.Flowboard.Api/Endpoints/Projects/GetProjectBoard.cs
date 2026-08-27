using Aurora.Flowboard.Application.Projects.GetBoard;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class GetProjectBoard : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "projects/{projectId:guid}/board",
            async (
                Guid projectId,
                IQueryHandler<GetProjectBoardQuery, IReadOnlyCollection<BoardColumnResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetProjectBoardQuery(projectId);

                Result<IReadOnlyCollection<BoardColumnResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetProjectBoard")
            .WithTags(EndpointTags.Projects)
            .Produces<IReadOnlyCollection<BoardColumnResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
