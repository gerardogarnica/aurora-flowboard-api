namespace Aurora.Flowboard.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    private const int DefaultMaxAttempts = 5;

    public int IntervalInSeconds { get; init; }
    public int BatchSize { get; init; }
    public int MaxAttempts { get; init; } = DefaultMaxAttempts;
}
