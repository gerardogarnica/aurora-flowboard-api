namespace Aurora.Flowboard.Application.WorkItems.GetByProject;

public sealed record WorkItemSummaryResponse(
    Guid WorkItemId,
    string Title,
    WorkItemType Type,
    Priority Priority,
    Guid FlowStateId,
    string FlowStateName,
    Guid? AssigneeId,
    int? EstimatedPoints,
    DateOnly? EstimatedCompletionDate,
    bool IsActive,
    DateTime CreatedOnUtc,
    int CommentCount,
    int TimeEntryCount);
