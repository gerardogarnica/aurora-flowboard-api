namespace Aurora.Flowboard.Domain.UnitTests.Projects;

internal static class ProjectData
{
    public const string Name = "Aurora Flowboard";
    public const string Description = "Project management API";
    public static readonly ProjectCode Prefix = ProjectCode.Create("AFB").Value;
    public const ProjectKind Kind = ProjectKind.Product;
    public static readonly Color Color = Color.Create("white").Value;
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime UpdatedOnUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public const string FlowStateName = "In Progress";
    public static readonly Color FlowStateColor = Color.Create("white").Value;

    public static readonly ProjectRole[] AllRoles =
        [ProjectRole.Admin, ProjectRole.Analyst, ProjectRole.Developer, ProjectRole.QA, ProjectRole.Viewer];

    public static Project GetProject(User? creator = null)
    {
        User user = creator ?? UserData.GetActiveUser();
        return Project.Create(Name, Description, Prefix, Kind, Color, user, CreatedOnUtc).Value;
    }

    public static Project GetProjectWithStatus(ProjectStatus status, User? admin = null, ProjectKind? kind = null)
    {
        User user = admin ?? UserData.GetActiveUser();
        ProjectKind resolvedKind = kind ?? (status == ProjectStatus.Completed ? ProjectKind.Client : Kind);
        Project project = Project.Create(Name, Description, Prefix, resolvedKind, Color, user, CreatedOnUtc).Value;

        switch (status)
        {
            case ProjectStatus.Active:
                break;
            case ProjectStatus.Maintenance:
                project.ChangeStatus(ProjectStatus.Maintenance, user, UpdatedOnUtc);
                break;
            case ProjectStatus.Completed:
                project.ChangeStatus(ProjectStatus.Completed, user, UpdatedOnUtc);
                break;
            case ProjectStatus.Archived:
                project.ChangeStatus(ProjectStatus.Archived, user, UpdatedOnUtc);
                break;
        }

        return project;
    }

    // A four-state flow (two active, one completed, one cancelled) with every role allowed on
    // every transition. Domain events are cleared so tests can assert on a single event.
    public static Project GetProjectWithFlowStates(User? admin = null)
    {
        User user = admin ?? UserData.GetActiveUser();
        Project project = GetProject(user);

        project.AddFlowState("Todo", FlowStateCategory.Active, FlowStateColor, AllRoles, user);
        project.AddFlowState("In Progress", FlowStateCategory.Active, FlowStateColor, AllRoles, user);
        project.AddFlowState("Done", FlowStateCategory.Completed, FlowStateColor, AllRoles, user);
        project.AddFlowState("Cancelled", FlowStateCategory.Cancelled, FlowStateColor, AllRoles, user);

        project.ClearDomainEvents();

        return project;
    }

    public static (Project Project, FlowTransition Transition) GetProjectWithFlowTransition(User? admin = null)
    {
        Project project = GetProjectWithFlowStates(admin);

        return (project, project.FlowTransitions.First());
    }

    // Returns a project whose status is Archived (CanModifyFlowStates returns false).
    public static (Project Project, User Admin) GetArchivedProjectWithFlowStates()
    {
        User admin = UserData.GetActiveUser();
        Project project = GetProjectWithFlowStates(admin);
        project.ChangeStatus(ProjectStatus.Archived, admin, UpdatedOnUtc);
        project.ClearDomainEvents();

        return (project, admin);
    }
}
