namespace Aurora.Flowboard.Application.Flows.GetAll;

public sealed record GetAllFlowsQuery(bool IncludeDeactivated, Guid? ProjectId) : IQuery<IReadOnlyCollection<FlowSummaryResponse>>;
