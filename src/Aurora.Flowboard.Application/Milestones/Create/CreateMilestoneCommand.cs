namespace Aurora.Flowboard.Application.Milestones.Create;

public sealed record CreateMilestoneCommand(
    Guid ProjectId,
    string Name,
    string? Description,
    string Color,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate) : ICommand<Guid>;
