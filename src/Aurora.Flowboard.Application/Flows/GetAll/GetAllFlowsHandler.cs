namespace Aurora.Flowboard.Application.Flows.GetAll;

internal sealed class GetAllFlowsHandler(
    IApplicationDbContext dbContext) : IQueryHandler<GetAllFlowsQuery, IReadOnlyCollection<FlowSummaryResponse>>
{
    public async Task<Result<IReadOnlyCollection<FlowSummaryResponse>>> Handle(
        GetAllFlowsQuery query,
        CancellationToken cancellationToken)
    {
        List<FlowSummaryResponse> flows = await dbContext
            .Flows
            .AsNoTracking()
            .Where(f => query.IncludeDeactivated || f.IsActive)
            .Where(f => query.ProjectId == null || f.ProjectId == query.ProjectId)
            .OrderBy(f => f.Name)
            .Select(f => new FlowSummaryResponse(
                f.Id,
                f.Name,
                f.Description,
                f.ProjectId,
                f.IsDefault,
                f.IsActive,
                f.States.Count,
                f.Transitions.Count))
            .ToListAsync(cancellationToken);

        return flows;
    }
}
