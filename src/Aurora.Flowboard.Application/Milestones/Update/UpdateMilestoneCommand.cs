namespace Aurora.Flowboard.Application.Milestones.Update;

public sealed record UpdateMilestoneCommand(
    Guid MilestoneId,
    string Name,
    string? Description,
    string Color,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate) : ICommand;
