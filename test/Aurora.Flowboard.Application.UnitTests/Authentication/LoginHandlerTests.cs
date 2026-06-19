using Aurora.Flowboard.Application.Authentication;
using Aurora.Flowboard.Application.Authentication.Login;

namespace Aurora.Flowboard.Application.UnitTests.Authentication;

public sealed class LoginHandlerTests
{
    private const string PlainPassword = "P@ssw0rd!";
    private const string HashedPassword = "hashed_password_123";
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenProvider = Substitute.For<ITokenProvider>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _handler = new LoginHandler(
            _dbContext,
            _passwordHasher,
            _tokenProvider,
            _dateTimeProvider);

        _dateTimeProvider.UtcNow.Returns(UtcNow);
    }

    [Fact]
    public async Task Should_ReturnIdentityToken_When_CredentialsAreValid()
    {
        // Arrange
        User user = CreateUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        _passwordHasher.VerifyHashedPassword(HashedPassword, PlainPassword).Returns(true);

        IdentityToken issued = CreateIdentityToken();
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(issued);

        var command = new LoginCommand("john.doe@example.com", PlainPassword);

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().Be(issued);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        user.Tokens.Should().ContainSingle(t => t.AccessToken == issued.AccessToken);
    }

    [Fact]
    public async Task Should_ReturnInvalidCredentials_When_EmailIsUnknown()
    {
        // Arrange
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        var command = new LoginCommand("missing@example.com", PlainPassword);

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidCredentials);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidCredentials_When_PasswordIsWrong()
    {
        // Arrange
        User user = CreateUser();
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _passwordHasher.VerifyHashedPassword(HashedPassword, Arg.Any<string>()).Returns(false);

        var command = new LoginCommand("john.doe@example.com", "wrong-password");

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidCredentials);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidCredentials_When_UserIsInactive()
    {
        // Arrange
        User user = CreateUser();
        user.Deactivate(UtcNow);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        var command = new LoginCommand("john.doe@example.com", PlainPassword);

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidCredentials);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_RunDummyHashVerification_When_EmailIsUnknown()
    {
        // Arrange — equalizes response time vs. wrong-password case to mitigate account enumeration.
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<User>());
        _dbContext.Users.Returns(usersMock);

        var command = new LoginCommand("missing@example.com", PlainPassword);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasher.Received(1).VerifyDummy(PlainPassword);
    }

    [Fact]
    public async Task Should_ReturnInvalidCredentials_When_EmailFormatIsInvalid()
    {
        // Arrange
        var command = new LoginCommand("not-an-email", PlainPassword);

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidCredentials);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PassUserRolesIntoTokenRequest_When_UserHasRoles()
    {
        // Arrange
        User user = CreateUser();
        user.AssignRole(Role.Administrator);
        user.AssignRole(Role.Member);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        _passwordHasher.VerifyHashedPassword(HashedPassword, PlainPassword).Returns(true);
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(CreateIdentityToken());

        var command = new LoginCommand("john.doe@example.com", PlainPassword);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenProvider.Received(1).CreateToken(Arg.Is<TokenRequest>(r =>
            r.UserId == user.Id &&
            r.Email == user.Email.Value &&
            r.Roles.Contains(Role.Administrator.Name) &&
            r.Roles.Contains(Role.Member.Name)));
    }

    private static User CreateUser()
    {
        Email email = Email.Create("john.doe@example.com").Value;
        Password password = Password.Create(HashedPassword).Value;
        return User.Create("John", "Doe", email, password, UtcNow).Value;
    }

    private static IdentityToken CreateIdentityToken() => new(
        AccessToken: "access-token-value",
        AccessTokenExpiresOn: new DateTimeOffset(UtcNow.AddMinutes(60), TimeSpan.Zero),
        RefreshToken: "refresh-token-value",
        RefreshTokenExpiresOn: new DateTimeOffset(UtcNow.AddDays(7), TimeSpan.Zero));
}
