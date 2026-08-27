namespace Aurora.Flowboard.Application.WorkItems.UpdateEstimatedPoints;

public sealed record UpdateWorkItemEstimatedPointsCommand(Guid Id, int? EstimatedPoints) : ICommand;
