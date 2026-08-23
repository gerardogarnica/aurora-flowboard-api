namespace Aurora.Flowboard.Application.WorkItems.UpdateType;

public sealed record UpdateWorkItemTypeCommand(Guid Id, WorkItemType Type) : ICommand;
