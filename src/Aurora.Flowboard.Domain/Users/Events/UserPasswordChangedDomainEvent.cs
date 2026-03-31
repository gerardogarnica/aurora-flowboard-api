namespace Aurora.Flowboard.Domain.Users.Events;

public sealed class UserPasswordChangedDomainEvent(Guid userId) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
}