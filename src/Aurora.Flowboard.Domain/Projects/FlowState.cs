namespace Aurora.Flowboard.Domain.Projects;

public sealed class FlowState
{
    public const int MaxNameLength = 50;

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }
    public int SortOrder { get; private set; }
    public FlowStateCategory Category { get; private set; }
    public Color Color { get; private set; }

    private FlowState() { } // EF Core

    private FlowState(Guid id, Guid projectId, string name, int sortOrder, FlowStateCategory category, Color color)
    {
        Id = id;
        ProjectId = projectId;
        Name = name;
        SortOrder = sortOrder;
        Category = category;
        Color = color;
    }

    internal static Result<FlowState> Create(Project project, string name, int sortOrder, FlowStateCategory category, Color color)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<FlowState>(ProjectErrors.FlowStateNameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail<FlowState>(ProjectErrors.FlowStateNameTooLong);
        }

        return new FlowState(
            Guid.NewGuid(),
            project.Id,
            name.Trim(),
            sortOrder,
            category,
            color);
    }

    internal void DecrementSortOrder() => SortOrder--;
}
