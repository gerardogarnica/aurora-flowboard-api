namespace Aurora.Flowboard.Application.WorkItems.GetByProject;

public sealed record GetWorkItemsByProjectQuery(
    Guid ProjectId,
    bool IncludeDeactivated) : IQuery<IReadOnlyCollection<WorkItemSummaryResponse>>;
