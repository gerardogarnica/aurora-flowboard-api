namespace Aurora.Flowboard.Application.Projects.GetChangeLogs;

public sealed record ProjectChangeLogResponse(
    Guid Id,
    Guid ChangedById,
    string ChangedByFullName,
    string ChangedByInitials,
    ProjectChangeType ChangeType,
    Guid? AffectedEntityId,
    string? AffectedEntityName,
    ProjectStatus? NewStatus,
    DateTime ChangedOnUtc);
