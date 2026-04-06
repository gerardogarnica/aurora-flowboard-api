namespace Aurora.Flowboard.Application.WorkItems.LogTime;

public sealed record LogWorkItemTimeCommand(
    Guid WorkItemId,
    Guid UserId,
    decimal Hours,
    string? Description,
    DateTime LoggedOnUtc) : ICommand;
