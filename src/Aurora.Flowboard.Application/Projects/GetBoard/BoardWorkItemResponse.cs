namespace Aurora.Flowboard.Application.Projects.GetBoard;

public sealed record BoardWorkItemResponse(
    Guid WorkItemId,
    string Code,
    string Title,
    WorkItemType Type,
    Priority Priority,
    Guid FlowStateId,
    string FlowStateName,
    Guid? AssigneeId,
    string? AssigneeInitials,
    string? AssigneeFullName,
    string? Component,
    string? Milestone,
    int? EstimatedPoints,
    DateOnly? EstimatedCompletionDate,
    DateTime CreatedOnUtc,
    int CommentCount,
    int TimeEntryCount);
