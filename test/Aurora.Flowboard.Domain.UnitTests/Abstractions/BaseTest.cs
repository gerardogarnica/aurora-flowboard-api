namespace Aurora.Flowboard.Domain.UnitTests.Abstractions;

public abstract class BaseTest
{
    protected static TDomainEvent AssertDomainEventWasPublished<TDomainEvent>(BaseEntity entity)
        where TDomainEvent : IDomainEvent
    {
        var domainEvent = entity.DomainEvents
            .OfType<TDomainEvent>()
            .SingleOrDefault();

        domainEvent.Should().NotBeNull();

        return domainEvent;
    }
}
