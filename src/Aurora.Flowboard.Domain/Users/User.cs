using Aurora.Flowboard.Domain.Users.Events;

namespace Aurora.Flowboard.Domain.Users;

public sealed class User : BaseEntity
{
    public const int MaxNameLength = 100;

    private readonly List<UserToken> _tokens = [];
    private readonly List<Role> _roles = [];

    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}";
    public string Initials => string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName)
        ? "U"
        : $"{char.ToUpperInvariant(FirstName[0])}{char.ToUpperInvariant(LastName[0])}";
    public Email Email { get; private set; }
    private Password Password { get; set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public IReadOnlyCollection<UserToken> Tokens => _tokens.AsReadOnly();
    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

    private User() : base(Guid.Empty) { } // EF Core

    private User(
        Guid id,
        string firstName,
        string lastName,
        Email email,
        Password password,
        DateTime createdOnUtc) : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Password = password;
        IsActive = true;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<User> Create(
        string firstName,
        string lastName,
        Email email,
        Password password,
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
            password,
            createdOnUtc);

        user.AddDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }

    public bool VerifyPassword(IPasswordHasher passwordHasher, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(passwordHasher);

        return passwordHasher.VerifyHashedPassword(Password.Hash, providedPassword);
    }

    public Result ChangePassword(Password newPassword, DateTime updatedOnUtc)
    {
        Password = newPassword;
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

    public Result<UserToken> IssueToken(
        string accessToken,
        string refreshToken,
        DateTime accessTokenExpiresOnUtc,
        DateTime refreshTokenExpiresOnUtc,
        DateTime issuedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Result.Fail<UserToken>(UserTokenErrors.AccessTokenRequired);
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Fail<UserToken>(UserTokenErrors.RefreshTokenRequired);
        }

        if (accessTokenExpiresOnUtc <= issuedOnUtc)
        {
            return Result.Fail<UserToken>(UserTokenErrors.InvalidExpiration);
        }

        if (refreshTokenExpiresOnUtc <= issuedOnUtc)
        {
            return Result.Fail<UserToken>(UserTokenErrors.InvalidExpiration);
        }

        var token = UserToken.Create(
            Id,
            accessToken,
            refreshToken,
            accessTokenExpiresOnUtc,
            refreshTokenExpiresOnUtc,
            issuedOnUtc);

        _tokens.Add(token);

        AddDomainEvent(new UserTokenIssuedDomainEvent(Id, token.UserTokenId));

        return token;
    }

    public Result RevokeToken(Guid userTokenId)
    {
        var token = _tokens.FirstOrDefault(t => t.UserTokenId == userTokenId);

        if (token is null)
        {
            return Result.Fail(UserTokenErrors.NotFound);
        }

        Result revokeResult = token.Revoke();

        if (!revokeResult.IsSuccessful)
        {
            return revokeResult;
        }

        AddDomainEvent(new UserTokenRevokedDomainEvent(Id, userTokenId));

        return Result.Ok();
    }

    public Result AssignRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (_roles.Any(r => r.Name == role.Name))
        {
            return Result.Fail(UserErrors.RoleAlreadyAssigned);
        }

        _roles.Add(role);

        AddDomainEvent(new UserRoleAssignedDomainEvent(Id, role.Name));

        return Result.Ok();
    }

    public Result RemoveRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);

        var existing = _roles.FirstOrDefault(r => r.Name == role.Name);

        if (existing is null)
        {
            return Result.Fail(UserErrors.RoleNotAssigned);
        }

        _roles.Remove(existing);

        AddDomainEvent(new UserRoleRemovedDomainEvent(Id, role.Name));

        return Result.Ok();
    }
}
