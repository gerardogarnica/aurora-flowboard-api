using System.Collections;
using System.Reflection;

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

    // Every role that can be assigned to a flow transition; Viewer is rejected by FlowStateDefinition.Create and Project.AddFlowState.
    public static readonly ProjectRole[] AllRoles =
        [ProjectRole.Admin, ProjectRole.Analyst, ProjectRole.Developer, ProjectRole.QA];

    // The smallest flow Project.Create accepts: one state per category. The names are not reused by
    // the flow tests, so they can add "State A", "Done" or "Cancelled" on top of it.
    public static readonly FlowStateDefinition[] InitialFlowStates =
    [
        GetFlowStateDefinition("Backlog", FlowStateCategory.Active),
        GetFlowStateDefinition("Closed", FlowStateCategory.Completed),
        GetFlowStateDefinition("Dropped", FlowStateCategory.Cancelled)
    ];

    // A four-state flow (two active, one completed, one cancelled) with every role allowed on
    // every transition.
    public static readonly FlowStateDefinition[] FourStateFlowStates =
    [
        GetFlowStateDefinition("Todo", FlowStateCategory.Active),
        GetFlowStateDefinition("In Progress", FlowStateCategory.Active),
        GetFlowStateDefinition("Done", FlowStateCategory.Completed),
        GetFlowStateDefinition("Cancelled", FlowStateCategory.Cancelled)
    ];

    public static FlowStateDefinition GetFlowStateDefinition(string name, FlowStateCategory category) =>
        FlowStateDefinition.Create(name, category, FlowStateColor, AllRoles).Value;

    // Domain events raised while building the project (creation and its initial flow) are cleared,
    // so tests can assert on the events of the operation under test.
    public static Project GetProject(User? creator = null)
    {
        User user = creator ?? UserData.GetActiveUser();
        Project project = Project.Create(Name, Description, Prefix, Kind, Color, InitialFlowStates, user, CreatedOnUtc).Value;
        project.ClearDomainEvents();

        return project;
    }

    // A project with no flow states, like those created before Project.Create required an initial
    // flow, which may still exist in the database. The domain cannot build one, so the private
    // collections are cleared by reflection.
    public static Project GetLegacyProjectWithoutFlow(User? creator = null)
    {
        Project project = GetProject(creator);
        ClearPrivateList(project, "_flowStates");
        ClearPrivateList(project, "_flowTransitions");

        return project;
    }

    private static void ClearPrivateList(Project project, string fieldName)
    {
        IList list = typeof(Project).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(project) as IList
            ?? throw new InvalidOperationException($"Project has no list field '{fieldName}'; update {nameof(GetLegacyProjectWithoutFlow)} if it was renamed.");
        list.Clear();
    }

    public static Project GetProjectWithStatus(ProjectStatus status, User? admin = null, ProjectKind? kind = null)
    {
        User user = admin ?? UserData.GetActiveUser();
        ProjectKind resolvedKind = kind ?? (status == ProjectStatus.Completed ? ProjectKind.Client : Kind);
        Project project = Project.Create(Name, Description, Prefix, resolvedKind, Color, InitialFlowStates, user, CreatedOnUtc).Value;

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

    // A project with FourStateFlowStates. Domain events are cleared so tests can assert on a single event.
    public static Project GetProjectWithFlowStates(User? admin = null)
    {
        User user = admin ?? UserData.GetActiveUser();
        Project project = Project.Create(Name, Description, Prefix, Kind, Color, FourStateFlowStates, user, CreatedOnUtc).Value;

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
