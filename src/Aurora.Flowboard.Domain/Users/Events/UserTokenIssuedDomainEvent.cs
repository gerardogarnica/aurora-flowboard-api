namespace Aurora.Flowboard.Domain.Users.Events;

public sealed class UserTokenIssuedDomainEvent(Guid userId, Guid userTokenId) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
    public Guid UserTokenId { get; init; } = userTokenId;
}
