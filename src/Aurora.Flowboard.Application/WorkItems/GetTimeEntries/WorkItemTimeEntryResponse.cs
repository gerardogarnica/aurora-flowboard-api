namespace Aurora.Flowboard.Application.WorkItems.GetTimeEntries;

public sealed record WorkItemTimeEntryResponse(
    Guid TimeEntryId,
    Guid UserId,
    string LoggedByFullName,
    decimal Hours,
    string? Description,
    DateTime LoggedOnUtc);
