namespace Aurora.Flowboard.Domain.Projects;

public sealed record ProjectCode
{
    private const int MaxLength = 3;

    public string Value { get; }

    private ProjectCode(string value) => Value = value;

    public static Result<ProjectCode> Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Result.Fail<ProjectCode>(ProjectErrors.CodeRequired);
        }

        string trimmed = input.Trim();

        if (trimmed.Length > MaxLength)
        {
            return Result.Fail<ProjectCode>(ProjectErrors.CodeTooLong);
        }

        if (!trimmed.All(char.IsLetter))
        {
            return Result.Fail<ProjectCode>(ProjectErrors.CodeInvalidCharacters);
        }

        return new ProjectCode(trimmed.ToUpperInvariant());
    }
}
