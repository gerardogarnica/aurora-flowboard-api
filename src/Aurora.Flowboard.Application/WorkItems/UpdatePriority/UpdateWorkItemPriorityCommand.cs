namespace Aurora.Flowboard.Application.WorkItems.UpdatePriority;

public sealed record UpdateWorkItemPriorityCommand(Guid Id, Priority Priority) : ICommand;
