using FluentValidation.Results;

namespace Aurora.Flowboard.Application.Abstractions.Behaviors;

internal static class ValidationBehavior
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(
            TCommand command,
            CancellationToken cancellationToken)
        {
            ValidationFailure[] failures = await ValidateAsync(command, validators);

            if (failures.Length == 0)
            {
                return await innerHandler.Handle(command, cancellationToken);
            }

            return Result.Fail<TResponse>(CreateValidationError(failures));
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            ValidationFailure[] failures = await ValidateAsync(command, validators);

            if (failures.Length == 0)
            {
                return await innerHandler.Handle(command, cancellationToken);
            }

            return Result.Fail(CreateValidationError(failures));
        }
    }

    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            ValidationFailure[] failures = await ValidateAsync(query, validators);

            if (failures.Length == 0)
            {
                return await innerHandler.Handle(query, cancellationToken);
            }

            return Result.Fail<TResponse>(CreateValidationError(failures));
        }
    }

    private static async Task<ValidationFailure[]> ValidateAsync<TCommand>(
        TCommand command,
        IEnumerable<IValidator<TCommand>> validators)
    {
        IValidator<TCommand>[] validatorArray = [.. validators];

        if (validatorArray.Length == 0)
        {
            return [];
        }

        ValidationContext<TCommand> context = new(command);
        List<ValidationFailure> failures = [];

        foreach (IValidator<TCommand> validator in validatorArray)
        {
            ValidationResult result = await validator.ValidateAsync(context);
            failures.AddRange(result.Errors);
        }

        return [.. failures.Distinct()];
    }

    private static ValidationError CreateValidationError(ValidationFailure[] validationFailures) =>
        new([.. validationFailures.Select(f => BaseError.Validation(f.ErrorCode, f.ErrorMessage))]);
}
