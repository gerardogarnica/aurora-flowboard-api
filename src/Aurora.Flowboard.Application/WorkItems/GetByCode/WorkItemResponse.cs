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
    IReadOnlyCollection<WorkItemCommentResponse> Comments,
    IReadOnlyCollection<WorkItemTimeEntryResponse> TimeEntries,
    IReadOnlyCollection<WorkItemStateTransitionResponse> StateHistory,
    IReadOnlyCollection<WorkItemChangeLogResponse> ChangeLogs,
    IReadOnlyCollection<WorkItemFlowTransitionResponse> AvailableTransitions);

public sealed record WorkItemTagResponse(
    Guid TagId,
    string Name);

public sealed record WorkItemCommentResponse(
    Guid CommentId,
    Guid AuthorId,
    string AuthorFullName,
    string Content,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);

public sealed record WorkItemTimeEntryResponse(
    Guid TimeEntryId,
    Guid UserId,
    decimal Hours,
    string? Description,
    DateTime LoggedOnUtc);

public sealed record WorkItemStateTransitionResponse(
    Guid StateTransitionId,
    Guid? FromStateId,
    string? FromStateName,
    Guid ToStateId,
    string ToStateName,
    Guid ChangedById,
    string ChangedByFullName,
    string? Reason,
    DateTime ChangedOnUtc);

public sealed record WorkItemChangeLogResponse(
    Guid ChangeLogId,
    Guid ChangedById,
    string ChangedByFullName,
    WorkItemChangeType ChangeType,
    Guid? AffectedEntityId,
    string? AffectedEntityName,
    DateTime ChangedOnUtc);

public sealed record WorkItemFlowTransitionResponse(
    Guid ToStateId,
    string ToStateName);
