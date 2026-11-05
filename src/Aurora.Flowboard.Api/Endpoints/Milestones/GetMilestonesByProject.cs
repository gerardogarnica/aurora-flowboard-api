using Aurora.Flowboard.Application.Milestones.GetByProject;

namespace Aurora.Flowboard.Api.Endpoints.Milestones;

public sealed class GetMilestonesByProject : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "projects/{id:guid}/milestones",
            async (
                Guid id,
                IQueryHandler<GetMilestonesByProjectQuery, IReadOnlyCollection<MilestoneResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetMilestonesByProjectQuery(id);

                Result<IReadOnlyCollection<MilestoneResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetMilestonesByProject")
            .WithTags(EndpointTags.Milestones)
            .Produces<IReadOnlyCollection<MilestoneResponse>>(StatusCodes.Status200OK)
            .ProducesStandardProblems(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status404NotFound);
    }
}
