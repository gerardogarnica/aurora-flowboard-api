using Aurora.Flowboard.Application.Components.GetByProject;

namespace Aurora.Flowboard.Api.Endpoints.Components;

public sealed class GetComponentsByProject : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "projects/{id:guid}/components",
            async (
                Guid id,
                IQueryHandler<GetComponentsByProjectQuery, IReadOnlyCollection<ComponentResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetComponentsByProjectQuery(id);

                Result<IReadOnlyCollection<ComponentResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetComponentsByProject")
            .WithTags(EndpointTags.Components)
            .Produces<IReadOnlyCollection<ComponentResponse>>(StatusCodes.Status200OK)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status404NotFound);
    }
}
