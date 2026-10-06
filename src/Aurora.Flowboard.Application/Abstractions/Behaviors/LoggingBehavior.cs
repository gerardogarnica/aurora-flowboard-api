using Microsoft.Extensions.Logging;

namespace Aurora.Flowboard.Application.Abstractions.Behaviors;

internal static class LoggingBehavior
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
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Processing request: {Name}", typeof(TCommand).Name);
            }

            Result<TResponse> result = await innerHandler.Handle(command, cancellationToken);

            if (result.IsSuccessful)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Request processed successfully: {Name}", typeof(TCommand).Name);
                }
            }
            else if (result.Error.ErrorType == BaseErrorType.Failure)
            {
                logger.LogError("Request processed with errors: {Name} {ErrorCode}", typeof(TCommand).Name, result.Error.Code);
            }
            else
            {
                logger.LogWarning("Request processed with errors: {Name} {ErrorCode}", typeof(TCommand).Name, result.Error.Code);
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
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Processing request: {Name}", typeof(TCommand).Name);
            }

            Result result = await innerHandler.Handle(command, cancellationToken);

            if (result.IsSuccessful)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Request processed successfully: {Name}", typeof(TCommand).Name);
                }
            }
            else if (result.Error.ErrorType == BaseErrorType.Failure)
            {
                logger.LogError("Request processed with errors: {Name} {ErrorCode}", typeof(TCommand).Name, result.Error.Code);
            }
            else
            {
                logger.LogWarning("Request processed with errors: {Name} {ErrorCode}", typeof(TCommand).Name, result.Error.Code);
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
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Processing request: {Name}", typeof(TQuery).Name);
            }

            Result<TResponse> result = await innerHandler.Handle(query, cancellationToken);

            if (result.IsSuccessful)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Request processed successfully: {Name}", typeof(TQuery).Name);
                }
            }
            else if (result.Error.ErrorType == BaseErrorType.Failure)
            {
                logger.LogError("Request processed with errors: {Name} {ErrorCode}", typeof(TQuery).Name, result.Error.Code);
            }
            else
            {
                logger.LogWarning("Request processed with errors: {Name} {ErrorCode}", typeof(TQuery).Name, result.Error.Code);
            }

            return result;
        }
    }
}
