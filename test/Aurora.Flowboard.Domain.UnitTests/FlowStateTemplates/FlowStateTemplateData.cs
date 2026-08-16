namespace Aurora.Flowboard.Domain.UnitTests.FlowStateTemplates;

internal static class FlowStateTemplateData
{
    public const string StateName = "In Progress";
    public const string UpdatedStateName = "In Review";

    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly Color Color = Color.Create("blue").Value;
    public static readonly Color UpdatedColor = Color.Create("green").Value;

    public static FlowStateTemplate GetTemplate(ProjectKind kind = ProjectKind.Product, Guid? createdBy = null) =>
        FlowStateTemplate.Create(kind, createdBy ?? Guid.NewGuid(), CreatedOnUtc).Value;
}
