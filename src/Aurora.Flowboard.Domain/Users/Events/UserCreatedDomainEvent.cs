namespace Aurora.Flowboard.Domain.Users.Events;

public sealed class UserCreatedDomainEvent(Guid userId) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
}