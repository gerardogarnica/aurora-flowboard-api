namespace Aurora.Flowboard.Application.WorkItems.AddTag;

public sealed record AddWorkItemTagCommand(
    Guid WorkItemId,
    string Name) : ICommand;
