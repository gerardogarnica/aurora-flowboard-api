namespace Aurora.Flowboard.Domain.Users.Events;

public sealed class UserRoleRemovedDomainEvent(Guid userId, string roleName) : DomainEvent
{
    public Guid UserId { get; init; } = userId;
    public string RoleName { get; init; } = roleName;
}
