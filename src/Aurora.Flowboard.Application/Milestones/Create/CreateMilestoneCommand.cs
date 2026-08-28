namespace Aurora.Flowboard.Application.Milestones.Create;

public sealed record CreateMilestoneCommand(
    Guid ProjectId,
    string Name,
    string? Description,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate) : ICommand<Guid>;
