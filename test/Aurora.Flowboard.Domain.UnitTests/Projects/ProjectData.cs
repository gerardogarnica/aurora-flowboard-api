namespace Aurora.Flowboard.Domain.UnitTests.Projects;

internal static class ProjectData
{
    public const string Name = "Aurora Flowboard";
    public const string Description = "Project management API";
    public const string Code = "AFB";
    public static readonly Color Color = Color.Create("white").Value;
    public static readonly DateOnly EstimatedCompletionDate = new(2026, 12, 31);
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime UpdatedOnUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Project GetDraftProject(User? creator = null)
    {
        User user = creator ?? UserData.GetActiveUser();
        return Project.Create(Name, Description, Code, Color, EstimatedCompletionDate, user, CreatedOnUtc).Value;
    }

    public static Project GetProjectWithStatus(ProjectStatus status, User? admin = null)
    {
        User user = admin ?? UserData.GetActiveUser();
        Project project = GetDraftProject(user);

        switch (status)
        {
            case ProjectStatus.Active:
                project.ChangeStatus(ProjectStatus.Active, user, UpdatedOnUtc);
                break;
            case ProjectStatus.OnHold:
                project.ChangeStatus(ProjectStatus.Active, user, UpdatedOnUtc);
                project.ChangeStatus(ProjectStatus.OnHold, user, UpdatedOnUtc);
                break;
            case ProjectStatus.Completed:
                project.ChangeStatus(ProjectStatus.Active, user, UpdatedOnUtc);
                project.ChangeStatus(ProjectStatus.Completed, user, UpdatedOnUtc);
                break;
            case ProjectStatus.Archived:
                project.ChangeStatus(ProjectStatus.Active, user, UpdatedOnUtc);
                project.ChangeStatus(ProjectStatus.Archived, user, UpdatedOnUtc);
                break;
        }

        return project;
    }
}
