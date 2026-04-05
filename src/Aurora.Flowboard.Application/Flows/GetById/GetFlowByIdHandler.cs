namespace Aurora.Flowboard.Application.Flows.GetById;

internal sealed class GetFlowByIdHandler(
    IApplicationDbContext dbContext) : IQueryHandler<GetFlowByIdQuery, FlowResponse>
{
    public async Task<Result<FlowResponse>> Handle(
        GetFlowByIdQuery query,
        CancellationToken cancellationToken)
    {
        Flow? flow = await dbContext
            .Flows
            .Include(f => f.States)
            .Include(f => f.Transitions)
            .AsNoTracking()
            .SingleOrDefaultAsync(f => f.Id == query.FlowId, cancellationToken);

        if (flow is null)
        {
            return Result.Fail<FlowResponse>(FlowErrors.NotFound);
        }

        Dictionary<Guid, string> stateNames = flow.States.ToDictionary(s => s.Id, s => s.Name);

        var response = new FlowResponse(
            flow.Id,
            flow.Name,
            flow.Description,
            flow.ProjectId,
            flow.IsDefault,
            flow.IsActive,
            flow.CreatedOnUtc,
            flow.UpdatedOnUtc,
            [.. flow.States.Select(s => new FlowStateResponse(
                s.Id,
                s.Name,
                s.SortOrder,
                s.IsTerminal))],
            [.. flow.Transitions.Select(t => new FlowTransitionResponse(
                t.Id,
                t.FromStateId,
                stateNames.GetValueOrDefault(t.FromStateId, string.Empty),
                t.ToStateId,
                stateNames.GetValueOrDefault(t.ToStateId, string.Empty),
                t.AllowedRole))]);

        return response;
    }
}
