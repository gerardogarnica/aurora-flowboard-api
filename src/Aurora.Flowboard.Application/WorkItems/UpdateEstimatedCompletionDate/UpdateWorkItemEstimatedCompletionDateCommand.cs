namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedCompletionDate;

public sealed record UpdateWorkItemEstimatedCompletionDateCommand(
    Guid Id,
    DateOnly? EstimatedCompletionDate) : ICommand;
