---
paths:
  - "src/Aurora.Flowboard.Infrastructure/Outbox/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/Interceptors/InsertOutboxMessagesInterceptor.cs"
  - "src/Aurora.Flowboard.Infrastructure/Serialization/**/*.cs"
  - "src/Aurora.Flowboard.Infrastructure/DomainEvents/**/*.cs"
  - "src/Aurora.Flowboard.Application/Abstractions/Messaging/IDomainEventHandler.cs"
  - "src/Aurora.Flowboard.Domain/**/Events/*.cs"
---

# Domain events and the outbox

## Flow

1. Entities raise events (`BaseEntity`).
2. `InsertOutboxMessagesInterceptor` writes them to `flowboard.outbox_messages` in the same `SaveChanges` as the change, so an event exists only if its change was committed.
3. `ProcessOutboxJob` (Quartz, `[DisallowConcurrentExecution]`, every `Outbox:IntervalInSeconds`) reads up to `BatchSize` pending messages `FOR UPDATE` in one transaction. It dispatches each one to its `IDomainEventHandler<T>` implementations (`DomainEventsDispatcher`, a new DI scope per event) and commits.

## Delivery is at-least-once: handlers must be idempotent

- A message that throws stays pending: `attempts` is incremented, `error` keeps the last exception, and the next run retries it.
- When `attempts` reaches `Outbox:MaxAttempts` (5 by default in `OutboxOptions`), the message is marked processed with its last error. That is a dead letter, and it is never retried.
- A message that succeeds after earlier failures is marked processed with `error` cleared, so its last error is lost on purpose: the dead-letter query below only returns messages that ran out of attempts. Its `attempts` is kept, so `is_processed AND attempts > 0 AND error = ''` lists the ones that needed retries.
- Only the run's own cancellation (`context.CancellationToken`, on shutdown) propagates, and it rolls back the whole batch, so the handlers that already ran see the event again. Any other exception from a handler is a failed attempt, including a `TaskCanceledException` from its own HTTP timeout. Letting that one propagate would roll back the attempt count too, so the message would never dead-letter and would block the queue.
- A handler can therefore receive the same event more than once. Make it idempotent: check before inserting, use the event `Id` as a key, or use upserts.
- A handler writes through its own scope's `DbContext`, outside the job's transaction. Its writes are not rolled back if a later message in the batch fails.
- There is no backoff: a failing message is retried every interval and keeps its batch slot until it dead-letters.
- Find dead letters with `SELECT * FROM flowboard.outbox_messages WHERE is_processed AND error <> '' ORDER BY occurred_on_utc DESC`.

## Serialization: flat events, allow-listed types

- Content is Json.NET with `TypeNameHandling.All` (`SerializerSettings`). `DomainEventSerializationBinder` only resolves the concrete `IDomainEvent` classes of the Domain assembly. Any other `$type` throws `JsonSerializationException`, which counts as a failed attempt and ends as a dead letter. This blocks arbitrary type instantiation from a tampered row.
- Keep events flat: `Guid`, `string`, enums and numbers. A nested object or collection gets its own `$type` and the binder rejects it on read. Extend the binder deliberately if an event ever needs one.
- Renaming or moving an event class breaks pending messages of the old name. Drain the outbox first, or keep the old type until it is empty.
- `Content` and `Error` are `varchar(4000)`, and the job truncates `Error` to fit.

## Job conventions

- Every call uses `context.CancellationToken` (Quartz shutdown) and `ToListAsync`, never `CancellationToken.None` or synchronous enumeration.
- Each run logs Information only when it processed messages. Start of run is Debug, a retryable failure is Warning, and a dead letter is Error.
- `OutboxOptions` is validated on start: `IntervalInSeconds`, `BatchSize` and `MaxAttempts` must be > 0.
