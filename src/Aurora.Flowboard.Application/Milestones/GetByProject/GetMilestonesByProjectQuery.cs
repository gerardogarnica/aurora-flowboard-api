namespace Aurora.Flowboard.Application.Milestones.GetByProject;

public sealed record GetMilestonesByProjectQuery(Guid ProjectId) : IQuery<IReadOnlyCollection<MilestoneResponse>>;
