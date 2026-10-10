namespace Aurora.Flowboard.Infrastructure.Authentication;

public sealed class UserTokenCleanupOptions
{
    public const string SectionName = "UserTokenCleanup";

    public int IntervalInHours { get; init; }
    public int RetentionDays { get; init; }
    public int BatchSize { get; init; }
}
