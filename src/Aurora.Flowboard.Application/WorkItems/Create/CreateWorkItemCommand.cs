namespace Aurora.Flowboard.Application.WorkItems.Create;

public sealed record CreateWorkItemCommand(
    string Title,
    string? Description,
    WorkItemType Type,
    Priority Priority,
    Guid ProjectId,
    Guid FlowStateId,
    Guid CreatedById,
    int? EstimatedPoints,
    DateOnly? EstimatedCompletionDate) : ICommand<Guid>;
