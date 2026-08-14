namespace Aurora.Flowboard.Application.UnitTests.Milestones;

internal static class MilestoneCommandData
{
    public const string Name = "Phase 1 delivery";
    public const string Description = "First delivery milestone";
    public const string UpdatedName = "Phase 1 launch";
    public const string UpdatedDescription = "Updated delivery milestone";
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly TargetStartDate = new(2026, 1, 15);
    public static readonly DateOnly TargetEndDate = new(2026, 2, 15);

    public static User GetAdmin()
    {
        Email email = Email.Create("admin@example.com").Value;
        return User.Create("Admin", "User", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

    public static User GetNonAdmin()
    {
        Email email = Email.Create("nonadmin@example.com").Value;
        return User.Create("Non", "Admin", email, Password.Create("hashed_password_456").Value, UtcNow).Value;
    }

    public static Project GetProject(User admin) =>
        Project.Create("Test Project", "Test Description", ProjectCode.Create("TST").Value, ProjectKind.Product, Color.Create("white").Value, admin, UtcNow).Value;

    public static Project GetProjectWithMilestone(User admin, out Milestone milestone)
    {
        Project project = GetProject(admin);
        milestone = Milestone.Create(Name, Description, TargetStartDate, TargetEndDate, project, admin, UtcNow).Value;
        return project;
    }

    public static CreateMilestoneCommand GetCreateCommand(Guid projectId) =>
        new(projectId, Name, Description, TargetStartDate, TargetEndDate);

    public static UpdateMilestoneCommand GetUpdateCommand(Guid milestoneId) =>
        new(milestoneId, UpdatedName, UpdatedDescription, TargetStartDate, TargetEndDate);

    public static ChangeMilestoneStatusCommand GetChangeStatusCommand(Guid milestoneId, MilestoneStatus newStatus) =>
        new(milestoneId, newStatus);
}
