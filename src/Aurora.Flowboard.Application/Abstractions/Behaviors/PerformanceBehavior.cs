using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Aurora.Flowboard.Application.Abstractions.Behaviors;

internal static class PerformanceBehavior
{
    private const int MaximumAllowedMilliseconds = 500;
    private const string LongRunningMessage = "Long-running request: {Name} ({ElapsedMilliseconds} milliseconds) {@Request}";

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TCommand command,
            CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Result<TResponse> result = await innerHandler.Handle(command, cancellationToken);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > MaximumAllowedMilliseconds)
            {
                logger.LogWarning(LongRunningMessage, typeof(TCommand).Name, stopwatch.ElapsedMilliseconds, command);
            }

            return result;
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandBaseHandler<TCommand>> logger) : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(
            TCommand command,
            CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Result result = await innerHandler.Handle(command, cancellationToken);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > MaximumAllowedMilliseconds)
            {
                logger.LogWarning(LongRunningMessage, typeof(TCommand).Name, stopwatch.ElapsedMilliseconds, command);
            }

            return result;
        }
    }

    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        ILogger<QueryHandler<TQuery, TResponse>> logger) : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TQuery query,
            CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Result<TResponse> result = await innerHandler.Handle(query, cancellationToken);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > MaximumAllowedMilliseconds)
            {
                logger.LogWarning(LongRunningMessage, typeof(TQuery).Name, stopwatch.ElapsedMilliseconds, query);
            }

            return result;
        }
    }
}
