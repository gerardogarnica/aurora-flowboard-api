namespace Aurora.Flowboard.Application.WorkItems.GetByProject;

public sealed record WorkItemSummaryResponse(
    Guid WorkItemId,
    string Title,
    string Code,
    WorkItemType Type,
    Priority Priority,
    Guid FlowStateId,
    string FlowStateName,
    Guid? AssigneeId,
    string? AssigneeInitials,
    string? AssigneeFullName,
    int? EstimatedPoints,
    DateOnly? EstimatedCompletionDate,
    DateTime CreatedOnUtc,
    int CommentCount,
    int TimeEntryCount);
