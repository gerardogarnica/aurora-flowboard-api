namespace Aurora.Flowboard.Application.WorkItems.GetWorkItems;

public sealed record WorkItemListItemResponse(
    Guid WorkItemId,
    string Code,
    string Title,
    WorkItemType Type,
    Priority Priority,
    Guid ProjectId,
    string ProjectName,
    string ProjectColor,
    Guid FlowStateId,
    string FlowStateName,
    FlowStateStage Stage,
    DateTime EnteredCurrentStateOnUtc,
    Guid? AssigneeId,
    string? AssigneeInitials,
    string? AssigneeFullName,
    string? MilestoneName,
    string? MilestoneColor);
