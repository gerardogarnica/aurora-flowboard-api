namespace Aurora.Flowboard.Domain.UnitTests.Milestones;

internal static class MilestoneData
{
    public const string Name = "Phase 1 delivery";
    public const string Description = "First delivery milestone";
    public const string UpdatedName = "Phase 1 launch";
    public const string UpdatedDescription = "Updated delivery milestone";
    public static readonly DateOnly TargetStartDate = new(2026, 1, 15);
    public static readonly DateOnly TargetEndDate = new(2026, 2, 15);
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime UpdatedOnUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Milestone GetMilestone(Project project, User admin, string? name = null) =>
        Milestone.Create(name ?? Name, Description, TargetStartDate, TargetEndDate, project, admin, CreatedOnUtc).Value;

    public static Milestone GetMilestoneWithStatus(MilestoneStatus status, Project project, User admin)
    {
        Milestone milestone = GetMilestone(project, admin);

        switch (status)
        {
            case MilestoneStatus.Draft:
                break;
            case MilestoneStatus.Active:
                milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, UpdatedOnUtc);
                break;
            case MilestoneStatus.OnHold:
                milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, UpdatedOnUtc);
                milestone.ChangeStatus(MilestoneStatus.OnHold, admin, 0, UpdatedOnUtc);
                break;
            case MilestoneStatus.Completed:
                milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, UpdatedOnUtc);
                milestone.ChangeStatus(MilestoneStatus.Completed, admin, 0, UpdatedOnUtc);
                break;
            case MilestoneStatus.Archived:
                milestone.ChangeStatus(MilestoneStatus.Archived, admin, 0, UpdatedOnUtc);
                break;
        }

        return milestone;
    }
}
