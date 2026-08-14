namespace Aurora.Flowboard.Application.UnitTests.Projects;

internal static class ProjectCommandData
{
    public const string Name = "Aurora Flowboard";
    public const string? Description = "Project management API";
    public const string Prefix = "AFB";
    public const ProjectKind Kind = ProjectKind.Product;
    public static readonly Color Color = Color.Create("white").Value;
    public const string UpdatedName = "Updated Flowboard";
    public const string? UpdatedDescription = "Updated project description";
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static User GetUser()
    {
        Email email = Email.Create("john.doe@example.com").Value;
        return User.Create("John", "Doe", email, Password.Create("hashed_password_123").Value, UtcNow).Value;
    }

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

    public static User GetNewUser()
    {
        Email email = Email.Create("newuser@example.com").Value;
        return User.Create("New", "User", email, Password.Create("hashed_password_789").Value, UtcNow).Value;
    }

    public static User GetMember()
    {
        Email email = Email.Create("member@example.com").Value;
        return User.Create("Regular", "Member", email, Password.Create("hashed_password_789").Value, UtcNow).Value;
    }

    public static Project GetProject(User admin) =>
        Project.Create("Test Project", "Test Description", ProjectCode.Create("TST").Value, Kind, Color, admin, UtcNow).Value;

    public static Project GetExistingProjectWithPrefix(User admin, string prefix = Prefix) =>
        Project.Create("Existing Project", "Existing Description", ProjectCode.Create(prefix).Value, Kind, Color, admin, UtcNow).Value;

    public static Project GetArchivedProject(User admin)
    {
        Project project = GetProject(admin);
        project.ChangeStatus(ProjectStatus.Archived, admin, UtcNow);
        return project;
    }

    public static Project GetProjectWithMember(User admin, User member)
    {
        Project project = GetProject(admin);
        project.AddMember(member, ProjectRole.Developer, admin, UtcNow);
        return project;
    }

    public static CreateProjectCommand GetCreateCommand() =>
        new(Name, Description, Prefix, Kind, Color);

    public static UpdateProjectCommand GetUpdateCommand(Guid projectId) =>
        new(projectId, UpdatedName, UpdatedDescription, Color);

    public static ChangeProjectStatusCommand GetChangeStatusCommand(Guid projectId) =>
        new(projectId, ProjectStatus.Maintenance);

    public static ChangeProjectKindCommand GetChangeKindCommand(Guid projectId) =>
        new(projectId, ProjectKind.Client);

    public static AddProjectMemberCommand GetAddMemberCommand(Guid projectId, Guid userId) =>
        new(projectId, userId, ProjectRole.Developer);

    public static RemoveProjectMemberCommand GetRemoveCommand(Guid projectId, Guid userId) =>
        new(projectId, userId);

    public const string FlowStateName = "In Review";
    public const string FlowStateColor = "white";

    public static Project GetProjectWithFlowStates(User admin)
    {
        Project project = GetProject(admin);
        Color stateColor = Color.Create(FlowStateColor).Value;
        ProjectRole[] allRoles = [ProjectRole.Admin, ProjectRole.Developer];

        project.AddFlowState("Backlog", FlowStateCategory.Active, stateColor, allRoles, admin);
        project.AddFlowState("Done", FlowStateCategory.Completed, stateColor, allRoles, admin);
        project.AddFlowState("Cancelled", FlowStateCategory.Cancelled, stateColor, allRoles, admin);

        return project;
    }

    public static (Project Project, FlowTransition Transition) GetProjectWithFlowTransition(User admin)
    {
        Project project = GetProjectWithFlowStates(admin);
        return (project, project.FlowTransitions.First());
    }
}
