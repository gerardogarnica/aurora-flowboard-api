namespace Aurora.Flowboard.Application.WorkItems.Create;

public sealed record CreateWorkItemCommand(
    string Title,
    string? Description,
    WorkItemType Type,
    Priority Priority,
    Guid ProjectId,
    Guid FlowId,
    int? EstimatedPoints,
    DateOnly? EstimatedCompletionDate,
    Guid? AssigneeId = null,
    Guid? MilestoneId = null,
    Guid? ComponentId = null) : ICommand<Guid>;
