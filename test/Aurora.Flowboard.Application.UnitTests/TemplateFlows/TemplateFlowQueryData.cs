namespace Aurora.Flowboard.Application.UnitTests.TemplateFlows;

internal static class TemplateFlowQueryData
{
    public static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly Guid CreatedBy = Guid.NewGuid();
    public static readonly Color Color = Color.Create("white").Value;

    public static TemplateFlow GetTemplateFlow(ProjectKind kind = ProjectKind.Product) =>
        TemplateFlow.Create(kind, CreatedBy, UtcNow).Value;

    public static TemplateFlow GetTemplateFlowWithStates(ProjectKind kind = ProjectKind.Product)
    {
        TemplateFlow template = GetTemplateFlow(kind);

        template.AddState("Backlog", FlowStateCategory.Active, Color);
        template.AddState("Done", FlowStateCategory.Completed, Color);
        template.AddState("Cancelled", FlowStateCategory.Cancelled, Color);

        return template;
    }
}
