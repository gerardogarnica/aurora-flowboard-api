namespace Aurora.Flowboard.Domain.Projects;

public sealed record ProjectCode
{
    public const int MaxLength = 3;

    public string Value { get; }

    private ProjectCode(string value) => Value = value;

    public static Result<ProjectCode> Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Result.Fail<ProjectCode>(ProjectErrors.PrefixRequired);
        }

        string trimmed = input.Trim();

        if (trimmed.Length > MaxLength)
        {
            return Result.Fail<ProjectCode>(ProjectErrors.PrefixTooLong);
        }

        if (!trimmed.All(char.IsLetter))
        {
            return Result.Fail<ProjectCode>(ProjectErrors.PrefixInvalidCharacters);
        }

        return new ProjectCode(trimmed.ToUpperInvariant());
    }

    public override string ToString() => Value;

    public static implicit operator string(ProjectCode prefix) => prefix.Value;
}
