# Value objects

Value objects are `sealed record` (structural equality), immutable, and validate in `Create`. Their errors live in a **separate** `{ValueObject}Errors.cs` file — never in the same file as the value object itself.

Shared value objects go in `Domain/Shared/`. One that belongs to a single aggregate (like `ProjectCode`) lives in that aggregate's folder.

## Template

```csharp
// src/{name}.Domain/Shared/Email.cs
namespace {name}.Domain.Shared;

public sealed record Email
{
    public const int MaxLength = 255;

    public string Value { get; init; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Fail<Email>(EmailErrors.Empty);
        }

        email = email.Trim().ToLowerInvariant();

        if (email.Length > MaxLength)
        {
            return Result.Fail<Email>(EmailErrors.TooLong);
        }

        if (!IsValidFormat(email))
        {
            return Result.Fail<Email>(EmailErrors.InvalidFormat);
        }

        return new Email(email);
    }

    private static bool IsValidFormat(string email)
    {
        int atIndex = email.IndexOf('@');
        int dotIndex = email.LastIndexOf('.');

        return atIndex > 0
            && dotIndex > atIndex + 1
            && dotIndex < email.Length - 1;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
```

## Its error catalog — separate file

```csharp
// src/{name}.Domain/Shared/EmailErrors.cs
namespace {name}.Domain.Shared;

public static class EmailErrors
{
    public static readonly BaseError Empty = BaseError.Validation(
        "Email.Empty",
        "Email cannot be empty");

    public static readonly BaseError TooLong = BaseError.Validation(
        "Email.TooLong",
        "Email cannot exceed 255 characters");

    public static readonly BaseError InvalidFormat = BaseError.Validation(
        "Email.InvalidFormat",
        "Email format is invalid");
}
```

## Rules

- `public const int MaxLength` on the value object — the EF configuration reuses it (`HasMaxLength(Email.MaxLength)`).
- Private constructor, `Create` is the only way in.
- Normalize before validating length (`Trim()`, `ToUpperInvariant()`, `ToLowerInvariant()`) so the stored form is canonical.
- `implicit operator string` and `ToString()` are optional conveniences — add them when the value object is frequently interpolated into strings.
- Consumers unwrap with `.Value` only after checking `.IsSuccessful`:

  ```csharp
  Result<Color> colorResult = Color.Create(command.Color);
  if (!colorResult.IsSuccessful)
  {
      return Result.Fail<Guid>(colorResult.Error);
  }
  ```

## Persistence

Mapped with `OwnsOne` and an explicit `HasColumnName` (the snake_case convention cannot infer the flattened name). See [ef-configuration.md](ef-configuration.md).
