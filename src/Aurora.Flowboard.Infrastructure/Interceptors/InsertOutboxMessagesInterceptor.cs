using Aurora.Flowboard.Infrastructure.Outbox;
using Aurora.Flowboard.Infrastructure.Serialization;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aurora.Flowboard.Infrastructure.Interceptors;

public sealed class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            InsertOutboxMessages(eventData.Context);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ClearDomainEvents(eventData.Context);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private static void InsertOutboxMessages(DbContext context)
    {
        List<OutboxMessage> outboxMessages = [.. context
            .ChangeTracker
            .Entries<BaseEntity>()
            .Select(x => x.Entity)
            .Where(x => x.DomainEvents.Count > 0)
            .SelectMany(x => x.DomainEvents)
            .Select(domainEvent => new OutboxMessage
            {
                Id = domainEvent.Id,
                Type = domainEvent.GetType().Name,
                Content = JsonConvert.SerializeObject(domainEvent, SerializerSettings.Instance),
                OccurredOnUtc = domainEvent.OccurredOnUtc,
                IsProcessed = false
            })];

        context.Set<OutboxMessage>().AddRange(outboxMessages);
    }

    private static void ClearDomainEvents(DbContext context)
    {
        foreach (BaseEntity entity in context
            .ChangeTracker
            .Entries<BaseEntity>()
            .Select(x => x.Entity)
            .Where(x => x.DomainEvents.Count > 0))
        {
            entity.ClearDomainEvents();
        }
    }
}
