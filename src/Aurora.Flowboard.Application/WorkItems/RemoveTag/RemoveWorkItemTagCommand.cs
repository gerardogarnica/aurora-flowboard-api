namespace Aurora.Flowboard.Application.WorkItems.RemoveTag;

public sealed record RemoveWorkItemTagCommand(
    Guid WorkItemId,
    Guid TagId) : ICommand;
