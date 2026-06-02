namespace Aurora.Flowboard.Domain.Shared;

public static class ColorErrors
{
    public static readonly BaseError ColorRequired = BaseError.Validation(
        "Color.Required",
        "Color is required");

    public static readonly BaseError ColorTooLong = BaseError.Validation(
        "Color.TooLong",
        "Color cannot exceed 20 characters");
}
