using System.Reflection;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

// Fixtures for the cross-project reads, GET work-items and GET dashboard. Times are relative to Now,
// so the time an item has spent in its state reads straight from the call.
internal static class GetWorkItemsQueryData
{
    public const string BacklogState = "Backlog";
    public const string InProgressState = "In Progress";
    public const string ReviewState = "Review";
    public const string DoneState = "Done";
    public const string CancelledState = "Cancelled";

    public static readonly DateTime Now = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly Color MilestoneColor = Color.Create("#F97316").Value;
    private static readonly DateTime CreatedOnUtc = Now.AddDays(-90);
    private static readonly Color ProjectColor = Color.Create("#3B82F6").Value;

    public static User GetUser(string firstName, string lastName)
    {
        Email email = Email.Create($"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@search.test").Value;
        User user = User.Create(firstName, lastName, email, Password.Create("hashed_password_123").Value, CreatedOnUtc).Value;
        user.AssignRole(Role.Member);
        return user;
    }

    // Backlog, In Progress and Review are Active, and Backlog is the backlog stage.
    // Client kind, so the project can reach Completed.
    public static Project GetThreeStageProject(string name, string code, User admin, params User[] developers) =>
        CreateProject(name, code, ProjectKind.Client, GetThreeStageFlow(), admin, developers);

    // The same flow with Product kind, so the project can reach Maintenance.
    public static Project GetContinuousThreeStageProject(string name, string code, User admin, params User[] developers) =>
        CreateProject(name, code, ProjectKind.Product, GetThreeStageFlow(), admin, developers);

    // Backlog is the only Active state: it is in flight, and the project has no backlog stage.
    public static Project GetSingleStateProject(string name, string code, User admin, params User[] developers) =>
        CreateProject(name, code, ProjectKind.Client, FlowStateDefinitionData.GetMinimalFlow(), admin, developers);

    public static void ChangeStatus(Project project, ProjectStatus status, User admin) =>
        EnsureSuccess(project.ChangeStatus(status, admin, 0, Now), $"{project.Name} to {status}");

    // Created in the project's first Active state, timeInState before Now.
    public static WorkItem CreateInBacklog(
        Project project,
        User creator,
        TimeSpan timeInState,
        Priority priority = Priority.Medium,
        User? assignee = null,
        Milestone? milestone = null) =>
        WorkItem.Create("Search item", null, WorkItemType.Story, priority, project, creator, null, null, Now - timeInState, assignee, milestone).Value;

    // Created long ago, then moved timeInState before Now to stateName along the flow's transitions.
    public static WorkItem CreateInState(
        Project project,
        string stateName,
        User creator,
        TimeSpan timeInState,
        Priority priority = Priority.Medium,
        User? assignee = null,
        Milestone? milestone = null)
    {
        WorkItem workItem = WorkItem.Create("Search item", null, WorkItemType.Story, priority, project, creator, null, null, CreatedOnUtc, assignee, milestone).Value;
        MoveTo(workItem, project, stateName, creator, Now - timeInState);
        return workItem;
    }

    public static Milestone CreateActiveMilestone(Project project, User admin, string name, DateOnly? targetEndDate, DateTime? createdOnUtc = null)
    {
        Milestone milestone = Milestone.Create(name, null, MilestoneColor, null, targetEndDate, project, admin, createdOnUtc ?? CreatedOnUtc).Value;
        EnsureSuccess(milestone.ChangeStatus(MilestoneStatus.Active, admin, 0, Now), $"{name} to Active");
        return milestone;
    }

    // Move cannot reopen an item (no transition leaves a closing state), but a row reopened by hand
    // keeps its CompletedOnUtc, which closedLast7Days must ignore.
    public static void SetCompletedOnUtc(WorkItem workItem, DateTime completedOnUtc) =>
        (typeof(WorkItem).GetField("<CompletedOnUtc>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("WorkItem has no CompletedOnUtc backing field."))
            .SetValue(workItem, completedOnUtc);

    // GetMySummaryHandler counts through Project.WorkItems, which EF fills and WorkItem.Create does not.
    public static void AttachWorkItems(Project project, params WorkItem[] workItems)
    {
        var list = (List<WorkItem>?)typeof(Project)
            .GetField("_workItems", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(project)
            ?? throw new InvalidOperationException("Project has no _workItems field.");
        list.AddRange(workItems);
    }

    private static void MoveTo(WorkItem workItem, Project project, string stateName, User changedBy, DateTime movedOnUtc)
    {
        FlowState target = project.FlowStates.Single(s => s.Name == stateName);

        if (target.Id == workItem.FlowStateId)
        {
            throw new InvalidOperationException($"The item is already in {stateName}: use {nameof(CreateInBacklog)}.");
        }

        // Active states only link to their neighbours, and Done only to the last Active state.
        int walkTo = target.Category switch
        {
            FlowStateCategory.Active => target.SortOrder,
            FlowStateCategory.Completed => project.FlowStates.Where(s => s.Category == FlowStateCategory.Active).Max(s => s.SortOrder),
            _ => workItem.FlowState.SortOrder
        };

        List<FlowState> steps = [.. project.FlowStates
            .Where(s => s.Category == FlowStateCategory.Active && s.SortOrder > workItem.FlowState.SortOrder && s.SortOrder <= walkTo)
            .OrderBy(s => s.SortOrder)];

        foreach (FlowState step in steps)
        {
            EnsureSuccess(workItem.Move(step, changedBy, null, movedOnUtc), $"move to {step.Name}");
        }

        if (target.Category != FlowStateCategory.Active)
        {
            EnsureSuccess(workItem.Move(target, changedBy, null, movedOnUtc), $"move to {target.Name}");
        }
    }

    private static Project CreateProject(string name, string code, ProjectKind kind, FlowStateDefinition[] flow, User admin, User[] developers)
    {
        Project project = Project.Create(name, null, ProjectCode.Create(code).Value, kind, ProjectColor, flow, admin, CreatedOnUtc).Value;

        foreach (User developer in developers)
        {
            EnsureSuccess(project.AddMember(developer, ProjectRole.Developer, admin, CreatedOnUtc), $"add {developer.FullName}");
        }

        return project;
    }

    private static FlowStateDefinition[] GetThreeStageFlow() =>
        FlowStateDefinitionData.GetFlow(
            (BacklogState, FlowStateCategory.Active),
            (InProgressState, FlowStateCategory.Active),
            (ReviewState, FlowStateCategory.Active),
            (DoneState, FlowStateCategory.Completed),
            (CancelledState, FlowStateCategory.Cancelled));

    private static void EnsureSuccess(Result result, string step)
    {
        if (!result.IsSuccessful)
        {
            throw new InvalidOperationException($"Fixture step '{step}' failed: {result.Error.Code}");
        }
    }
}
