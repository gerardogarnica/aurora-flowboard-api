namespace Aurora.Flowboard.Application.WorkItems.GetChangeLogs;

public sealed record WorkItemChangeLogResponse(
    Guid ChangeLogId,
    Guid ChangedById,
    string ChangedByFullName,
    WorkItemChangeType ChangeType,
    Guid? AffectedEntityId,
    string? AffectedEntityName,
    DateTime ChangedOnUtc);
