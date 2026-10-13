namespace Aurora.Flowboard.Domain.Users.Events;

public sealed class UserReactivatedDomainEvent(Guid userId) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
}
