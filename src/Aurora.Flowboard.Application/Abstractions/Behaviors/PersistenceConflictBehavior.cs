using System.Data.Common;
using Microsoft.Extensions.Logging;

namespace Aurora.Flowboard.Application.Abstractions.Behaviors;

internal static class PersistenceConflictBehavior
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TCommand command,
            CancellationToken cancellationToken)
        {
            try
            {
                return await innerHandler.Handle(command, cancellationToken);
            }
            catch (Exception exception) when (GetConflictError(exception) is { } error)
            {
                LogConflict(logger, typeof(TCommand).Name, error, exception);

                return Result.Fail<TResponse>(error);
            }
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandBaseHandler<TCommand>> logger) : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            try
            {
                return await innerHandler.Handle(command, cancellationToken);
            }
            catch (Exception exception) when (GetConflictError(exception) is { } error)
            {
                LogConflict(logger, typeof(TCommand).Name, error, exception);

                return Result.Fail(error);
            }
        }
    }

    // A serialization failure or a deadlock surfaces directly from a query or CommitAsync, or wrapped by
    // SaveChanges; Postgres rolls the transaction back in both cases, so the client can retry.
    // DbException (System.Data.Common) exposes SqlState without depending on Npgsql.
    private static BaseError? GetConflictError(Exception exception) => exception switch
    {
        DbUpdateConcurrencyException => PersistenceErrors.ConcurrencyConflict,
        DbException { SqlState: PersistenceErrors.SerializationFailureSqlState or PersistenceErrors.DeadlockDetectedSqlState }
            => PersistenceErrors.ConcurrencyConflict,
        DbUpdateException
        {
            InnerException: DbException
            {
                SqlState: PersistenceErrors.SerializationFailureSqlState or PersistenceErrors.DeadlockDetectedSqlState
            }
        } => PersistenceErrors.ConcurrencyConflict,
        DbUpdateException { InnerException: DbException { SqlState: PersistenceErrors.UniqueViolationSqlState } }
            => PersistenceErrors.UniqueViolation,
        _ => null
    };

    // The exception keeps the constraint name and the failed statement, which the Result loses.
    private static void LogConflict(ILogger logger, string commandName, BaseError error, Exception exception) =>
        logger.LogWarning(exception, "Persistence conflict in {Name}: {ErrorCode}", commandName, error.Code);
}
