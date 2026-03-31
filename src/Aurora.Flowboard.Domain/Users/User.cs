using Aurora.Flowboard.Domain.Users.Events;

namespace Aurora.Flowboard.Domain.Users;

public sealed class User : BaseEntity
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}";
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    private User() : base(Guid.Empty) { } // EF Core

    private User(
        Guid id,
        string firstName,
        string lastName,
        Email email,
        string passwordHash,
        DateTime createdOnUtc) : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        IsActive = true;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<User> Create(
        string firstName,
        string lastName,
        Email email,
        string passwordHash,
        DateTime createdOnUtc)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Result.Fail<User>(UserErrors.FirstNameRequired);
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Result.Fail<User>(UserErrors.LastNameRequired);
        }

        var user = new User(
            Guid.NewGuid(),
            firstName.Trim(),
            lastName.Trim(),
            email,
            passwordHash,
            createdOnUtc);

        user.AddDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }

    public Result ChangePassword(string newPasswordHash, DateTime updatedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            return Result.Fail(UserErrors.PasswordHashRequired);
        }

        PasswordHash = newPasswordHash;
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new UserPasswordChangedDomainEvent(Id));

        return Result.Ok();
    }

    public Result Deactivate(DateTime updatedOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(UserErrors.AlreadyDeactivated);
        }

        IsActive = false;
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new UserDeactivatedDomainEvent(Id));

        return Result.Ok();
    }
}
