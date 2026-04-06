namespace Aurora.Flowboard.Application.WorkItems.GetById;

public sealed record WorkItemResponse(
    Guid WorkItemId,
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
    int? EstimatedPoints,
    DateOnly? EstimatedCompletionDate,
    bool IsActive,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc,
    DateTime? CompletedOnUtc,
    IReadOnlyCollection<WorkItemCommentResponse> Comments,
    IReadOnlyCollection<WorkItemTimeEntryResponse> TimeEntries,
    IReadOnlyCollection<WorkItemStateTransitionResponse> StateHistory,
    IReadOnlyCollection<WorkItemChangeLogResponse> ChangeLogs);

public sealed record WorkItemCommentResponse(
    Guid CommentId,
    Guid AuthorId,
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
    Guid ToStateId,
    Guid ChangedById,
    string? Reason,
    DateTime ChangedOnUtc);

public sealed record WorkItemChangeLogResponse(
    Guid ChangeLogId,
    Guid ChangedById,
    WorkItemChangeType ChangeType,
    Guid? AffectedEntityId,
    DateTime ChangedOnUtc);
