using Microsoft.Extensions.Logging;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions;

internal sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly List<CapturedLogEntry> _entries = [];

    public IReadOnlyList<CapturedLogEntry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        // Exporters stringify structured values, so capture them the same way.
        List<string> values = state is IEnumerable<KeyValuePair<string, object?>> properties
            ? [.. properties.Select(p => p.Value?.ToString() ?? string.Empty)]
            : [];

        _entries.Add(new CapturedLogEntry(logLevel, formatter(state, exception), values));
    }
}
