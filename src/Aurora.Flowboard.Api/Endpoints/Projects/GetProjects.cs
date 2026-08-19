using Aurora.Flowboard.Application.Projects.GetAll;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class GetProjects : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "projects",
            async (
                IQueryHandler<GetAllProjectsQuery, IReadOnlyCollection<ProjectSummaryResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetAllProjectsQuery();

                Result<IReadOnlyCollection<ProjectSummaryResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetProjects")
            .WithTags(EndpointTags.Projects)
            .Produces<IReadOnlyCollection<ProjectSummaryResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
