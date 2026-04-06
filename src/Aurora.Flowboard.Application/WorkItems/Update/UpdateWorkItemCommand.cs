namespace Aurora.Flowboard.Application.WorkItems.Update;

public sealed record UpdateWorkItemCommand(
    Guid Id,
    string Title,
    string? Description,
    Priority Priority,
    Guid ChangedById,
    int? EstimatedPoints,
    DateOnly? EstimatedCompletionDate) : ICommand;
