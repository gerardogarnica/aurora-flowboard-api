namespace Aurora.Flowboard.Application.WorkItems.Assign;

public sealed record AssignWorkItemCommand(
    Guid Id,
    Guid AssigneeId,
    Guid ChangedById) : ICommand;
