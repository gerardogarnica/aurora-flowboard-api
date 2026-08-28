namespace Aurora.Flowboard.Application.Milestones.ChangeStatus;

public sealed record ChangeMilestoneStatusCommand(Guid MilestoneId, MilestoneStatus NewStatus) : ICommand;
