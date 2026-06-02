namespace Aurora.Flowboard.Domain.Shared;

public sealed record Color
{
    public const int MaxLength = 20;

    public string Value { get; init; }

    private Color(string value)
    {
        Value = value;
    }

    public static Result<Color> Create(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return Result.Fail<Color>(ColorErrors.ColorRequired);
        }

        color = color.Trim();

        if (color.Length > MaxLength)
        {
            return Result.Fail<Color>(ColorErrors.ColorTooLong);
        }

        return new Color(color);
    }

    public override string ToString() => Value;

    public static implicit operator string(Color color) => color.Value;
}
