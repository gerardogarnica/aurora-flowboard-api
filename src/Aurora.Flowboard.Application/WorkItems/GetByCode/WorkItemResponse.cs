namespace Aurora.Flowboard.Application.WorkItems.GetByCode;

public sealed record WorkItemResponse(
    Guid WorkItemId,
    string Code,
    string Title,
    string? Description,
    WorkItemType Type,
    Priority Priority,
    Guid ProjectId,
    string ProjectName,
    Guid FlowStateId,
    string FlowStateName,
    Guid? AssigneeId,
    string? AssigneeFullName,
    Guid CreatedById,
    string CreatedByFullName,
    Guid? ComponentId,
    string? ComponentName,
    Guid? MilestoneId,
    string? MilestoneName,
    int? EstimatedPoints,
    DateOnly? EstimatedCompletionDate,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc,
    DateTime? CompletedOnUtc,
    IReadOnlyCollection<WorkItemTagResponse> Tags,
    IReadOnlyCollection<WorkItemFlowTransitionResponse> AvailableTransitions);

public sealed record WorkItemTagResponse(
    Guid TagId,
    string Name);

public sealed record WorkItemFlowTransitionResponse(
    Guid ToStateId,
    string ToStateName);
