namespace Aurora.Flowboard.Application.WorkItems.LogTime;

public sealed record LogWorkItemTimeCommand(
    Guid WorkItemId,
    decimal Hours,
    string? Description,
    DateTime LoggedOnUtc) : ICommand;
