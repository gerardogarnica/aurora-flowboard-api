namespace Aurora.Flowboard.Application.Milestones.GetByProject;

public sealed record MilestoneResponse(
    Guid Id,
    string Name,
    string? Description,
    MilestoneStatus Status,
    DateOnly? TargetStartDate,
    DateOnly? TargetEndDate,
    Guid CreatedBy,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
