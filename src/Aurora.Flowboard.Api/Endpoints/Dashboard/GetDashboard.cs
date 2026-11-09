using Aurora.Flowboard.Application.Dashboard.GetDashboard;

namespace Aurora.Flowboard.Api.Endpoints.Dashboard;

public sealed class GetDashboard : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "dashboard",
            async (
                IQueryHandler<GetDashboardQuery, DashboardResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<DashboardResponse> result = await handler.Handle(new GetDashboardQuery(), cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetDashboard")
            .WithTags(EndpointTags.Dashboard)
            .Produces<DashboardResponse>(StatusCodes.Status200OK)
            .ProducesStandardProblems();
    }
}
