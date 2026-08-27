namespace Aurora.Flowboard.Domain.UnitTests.Components;

internal static class ComponentData
{
    public const string Name = "Billing";
    public const string RenamedTo = "Payments";
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime UpdatedOnUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Component GetComponent(Project project, User admin, string? name = null) =>
        Component.Create(name ?? Name, project, admin, CreatedOnUtc).Value;
}
