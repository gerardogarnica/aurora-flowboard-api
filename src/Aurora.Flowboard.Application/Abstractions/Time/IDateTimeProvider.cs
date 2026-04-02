namespace Aurora.Flowboard.Application.Abstractions.Time;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateOnly Today { get; }
}
