namespace Aurora.Flowboard.Application.UnitTests.Projects;

internal static class ProjectCommandData
{
    public const string Name = "Aurora Flowboard";
    public const string? Description = "Project management API";
    public const string Code = "AFB";
    public static readonly Color Color = Color.Create("white").Value;
    public const string UpdatedName = "Updated Flowboard";
    public const string? UpdatedDescription = "Updated project description";
    public static readonly DateOnly EstimatedCompletionDate = new(2026, 12, 31);
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly Today = DateOnly.FromDateTime(UtcNow);

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

    public static Project GetDraftProject(User admin) =>
        Project.Create("Test Project", "Test Description", "TST", Color, null, admin, UtcNow).Value;

    public static Project GetArchivedProject(User admin)
    {
        Project project = GetDraftProject(admin);
        project.ChangeStatus(ProjectStatus.Archived, admin, UtcNow);
        return project;
    }

    public static Project GetProjectWithMember(User admin, User member)
    {
        Project project = GetDraftProject(admin);
        project.AddMember(member, ProjectRole.Developer, admin, UtcNow);
        return project;
    }

    public static CreateProjectCommand GetCreateCommand() =>
        new(Name, Description, Code, Color, EstimatedCompletionDate);

    public static UpdateProjectCommand GetUpdateCommand(Guid projectId) =>
        new(projectId, UpdatedName, UpdatedDescription, Color, EstimatedCompletionDate);

    public static ChangeProjectStatusCommand GetChangeStatusCommand(Guid projectId) =>
        new(projectId, ProjectStatus.Active);

    public static AddProjectMemberCommand GetAddMemberCommand(Guid projectId, Guid userId) =>
        new(projectId, userId, ProjectRole.Developer);

    public static RemoveProjectMemberCommand GetRemoveCommand(Guid projectId, Guid userId) =>
        new(projectId, userId);

    public static SetupProjectFlowDto GetSetupFlowDto() =>
        new("Sprint Flow", null,
        [
            new("Todo", FlowStateCategory.Active, "white", [ProjectRole.Developer]),
            new("Done", FlowStateCategory.Completed, "white", [ProjectRole.Developer]),
            new("Cancelled", FlowStateCategory.Cancelled, "white", [ProjectRole.Developer])
        ]);

    public static SetupProjectCommand GetSetupCommand() =>
        new(Name, Description, Code, Color, EstimatedCompletionDate, GetSetupFlowDto());
}
