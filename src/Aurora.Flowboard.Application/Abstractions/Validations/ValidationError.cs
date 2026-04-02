namespace Aurora.Flowboard.Application.Abstractions.Validations;

public sealed record ValidationError : BaseError
{
    public BaseError[] Errors { get; }

    public ValidationError(BaseError[] errors)
        : base("Validation", "One or more validation errors occurred", BaseErrorType.Validation)
    {
        Errors = errors;
    }

    public static ValidationError FromResults(IEnumerable<Result> results) =>
        new([.. results.Where(r => !r.IsSuccessful).Select(r => r.Error)]);
}
