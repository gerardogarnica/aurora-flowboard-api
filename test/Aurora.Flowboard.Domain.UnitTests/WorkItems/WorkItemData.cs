using System.Reflection;

namespace Aurora.Flowboard.Domain.UnitTests.WorkItems;

internal static class WorkItemData
{
    public const string Title = "Implement authentication";
    public const string Description = "Add JWT-based authentication";
    public const WorkItemType Type = WorkItemType.Story;
    public const Priority Priority = Priority.Medium;
    public const int EstimatedPoints = 5;
    public static readonly DateOnly EstimatedCompletionDate = new(2026, 6, 30);
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime UpdatedOnUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public static (Project Project, Flow Flow, User Admin) GetActiveProjectWithFlow()
    {
        User admin = UserData.GetActiveUser();
        Project project = ProjectData.GetDraftProject(admin);
        project.ChangeStatus(ProjectStatus.Active, admin, ProjectData.UpdatedOnUtc);

        Flow flow = Flow.Create(FlowData.Name, FlowData.Description, project, true, admin, FlowData.CreatedOnUtc).Value;

        ProjectRole[] allRoles = [ProjectRole.Admin, ProjectRole.Analyst, ProjectRole.Developer, ProjectRole.QA, ProjectRole.Support];
        Color stateColor = FlowData.Color;
        flow.AddState("Todo", FlowStateCategory.Active, stateColor, allRoles, admin);
        flow.AddState("In Progress", FlowStateCategory.Active, stateColor, allRoles, admin);
        flow.AddState("Done", FlowStateCategory.Completed, stateColor, allRoles, admin);
        flow.AddState("Cancelled", FlowStateCategory.Cancelled, stateColor, allRoles, admin);

        // Set Flow nav property on all FlowStates so Move() can call FlowState.Flow.FindTransition()
        foreach (FlowState state in flow.States)
        {
            SetFlowNavProperty(state, flow);
        }

        return (project, flow, admin);
    }

    public static WorkItem GetWorkItem()
    {
        var (project, flow, admin) = GetActiveProjectWithFlow();

        return WorkItem.Create(
            Title,
            Description,
            Type,
            Priority,
            project,
            flow,
            admin,
            EstimatedPoints,
            EstimatedCompletionDate,
            CreatedOnUtc).Value;
    }

    public static (WorkItem WorkItem, Project Project, Flow Flow, User Admin) GetWorkItemWithContext()
    {
        var (project, flow, admin) = GetActiveProjectWithFlow();

        WorkItem workItem = WorkItem.Create(
            Title,
            Description,
            Type,
            Priority,
            project,
            flow,
            admin,
            EstimatedPoints,
            EstimatedCompletionDate,
            CreatedOnUtc).Value;

        return (workItem, project, flow, admin);
    }

    internal static void SetFlowNavProperty(FlowState state, Flow flow) =>
        typeof(FlowState)
            .GetField("<Flow>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(state, flow);
}
