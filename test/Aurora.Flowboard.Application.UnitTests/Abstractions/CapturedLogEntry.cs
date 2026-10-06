using Microsoft.Extensions.Logging;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions;

internal sealed record CapturedLogEntry(LogLevel Level, string Message, IReadOnlyList<string> Values)
{
    public bool Contains(string text) =>
        Message.Contains(text, StringComparison.Ordinal)
        || Values.Any(v => v.Contains(text, StringComparison.Ordinal));
}
