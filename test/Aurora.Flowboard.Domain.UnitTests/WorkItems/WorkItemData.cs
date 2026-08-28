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

    public static (Project Project, User Admin) GetActiveProjectWithFlow()
    {
        User admin = UserData.GetActiveUser();
        Project project = ProjectData.GetProjectWithFlowStates(admin);

        return (project, admin);
    }

    public static WorkItem GetWorkItem()
    {
        var (project, admin) = GetActiveProjectWithFlow();

        return WorkItem.Create(
            Title,
            Description,
            Type,
            Priority,
            project,
            admin,
            EstimatedPoints,
            EstimatedCompletionDate,
            CreatedOnUtc).Value;
    }

    public static (WorkItem WorkItem, Project Project, User Admin) GetWorkItemWithContext()
    {
        var (project, admin) = GetActiveProjectWithFlow();

        WorkItem workItem = WorkItem.Create(
            Title,
            Description,
            Type,
            Priority,
            project,
            admin,
            EstimatedPoints,
            EstimatedCompletionDate,
            CreatedOnUtc).Value;

        return (workItem, project, admin);
    }
}
