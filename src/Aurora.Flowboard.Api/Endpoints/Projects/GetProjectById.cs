using Aurora.Flowboard.Application.Projects.GetById;

namespace Aurora.Flowboard.Api.Endpoints.Projects;

public sealed class GetProjectById : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "projects/{id:guid}",
            async (
                Guid id,
                IQueryHandler<GetProjectByIdQuery, ProjectResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetProjectByIdQuery(id);

                Result<ProjectResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetProjectById")
            .WithTags(EndpointTags.Projects)
            .Produces<ProjectResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
