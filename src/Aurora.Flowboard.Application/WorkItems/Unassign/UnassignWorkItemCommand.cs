namespace Aurora.Flowboard.Application.WorkItems.Unassign;

public sealed record UnassignWorkItemCommand(Guid Id) : ICommand;
