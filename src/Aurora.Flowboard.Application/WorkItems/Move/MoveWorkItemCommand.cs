namespace Aurora.Flowboard.Application.WorkItems.Move;

public sealed record MoveWorkItemCommand(
    Guid Id,
    Guid ToStateId,
    string? Reason) : ICommand;
