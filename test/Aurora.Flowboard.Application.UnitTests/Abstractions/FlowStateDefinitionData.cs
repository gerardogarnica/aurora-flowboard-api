namespace Aurora.Flowboard.Application.UnitTests.Abstractions;

// Builds the initial flow that Project.Create requires.
internal static class FlowStateDefinitionData
{
    private static readonly Color StateColor = Color.Create("white").Value;
    private static readonly ProjectRole[] DefaultRoles = [ProjectRole.Admin, ProjectRole.Developer];

    // Backlog (Active), Done (Completed) and Cancelled (Cancelled).
    public static FlowStateDefinition[] GetMinimalFlow() =>
        GetFlow(
            ("Backlog", FlowStateCategory.Active),
            ("Done", FlowStateCategory.Completed),
            ("Cancelled", FlowStateCategory.Cancelled));

    public static FlowStateDefinition[] GetFlow(params (string Name, FlowStateCategory Category)[] states) =>
        [.. states.Select(s => FlowStateDefinition.Create(s.Name, s.Category, StateColor, DefaultRoles).Value)];
}
