namespace Aurora.Flowboard.Domain.Projects;

public sealed record FlowStateDefinition : IValueObject
{
    public string Name { get; }
    public FlowStateCategory Category { get; }
    public Color Color { get; }
    public IReadOnlyCollection<ProjectRole> AllowedRoles { get; }

    private FlowStateDefinition(
        string name,
        FlowStateCategory category,
        Color color,
        IReadOnlyCollection<ProjectRole> allowedRoles)
    {
        Name = name;
        Category = category;
        Color = color;
        AllowedRoles = [.. allowedRoles];
    }

    public static Result<FlowStateDefinition> Create(
        string name,
        FlowStateCategory category,
        Color color,
        IReadOnlyCollection<ProjectRole> allowedRoles)
    {
        if (allowedRoles.Contains(ProjectRole.Viewer))
        {
            return Result.Fail<FlowStateDefinition>(ProjectErrors.FlowViewerRoleNotAllowed);
        }

        return new FlowStateDefinition(name, category, color, allowedRoles);
    }
}
