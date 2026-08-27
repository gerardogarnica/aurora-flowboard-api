namespace Aurora.Flowboard.Application.WorkItems.ChangeMilestone;

public sealed record ChangeWorkItemMilestoneCommand(Guid Id, Guid? MilestoneId) : ICommand;
