namespace Aurora.Flowboard.Application.WorkItems.Move;

public sealed record MoveWorkItemCommand(
    Guid Id,
    Guid ToStateId,
    Guid ChangedById,
    string? Reason) : ICommand;
