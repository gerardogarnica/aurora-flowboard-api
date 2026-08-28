namespace Aurora.Flowboard.Infrastructure.Bootstrap;

public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    public required string AdminEmail { get; init; }

    public required string AdminPassword { get; init; }

    public string AdminFirstName { get; init; } = "System";

    public string AdminLastName { get; init; } = "Administrator";
}
