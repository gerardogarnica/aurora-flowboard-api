namespace Aurora.Flowboard.Domain.Users.Events;

public sealed class UserDeactivatedDomainEvent(Guid userId) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
}