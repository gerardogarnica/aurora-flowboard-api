using Aurora.Flowboard.Application.Authentication;
using Aurora.Flowboard.Application.Authentication.RefreshToken;

namespace Aurora.Flowboard.Application.UnitTests.Authentication;

public sealed class RefreshTokenHandlerTests
{
    private const string HashedPassword = "hashed_password_123";
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IApplicationDbContext _dbContext;
    private readonly ITokenProvider _tokenProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly RefreshTokenHandler _handler;

    public RefreshTokenHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tokenProvider = Substitute.For<ITokenProvider>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _handler = new RefreshTokenHandler(
            _dbContext,
            _tokenProvider,
            _dateTimeProvider);

        _dateTimeProvider.UtcNow.Returns(UtcNow);
    }

    [Fact]
    public async Task Should_ReturnNewIdentityToken_When_RefreshTokenIsValid()
    {
        // Arrange
        User user = CreateUser();
        UserToken oldToken = IssueValidToken(user);

        DbSet<UserToken> userTokensMock = MockDbSetHelper.CreateMockDbSet([oldToken]);
        _dbContext.UserTokens.Returns(userTokensMock);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        IdentityToken issued = CreateIdentityToken();
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(issued);

        var command = new RefreshTokenCommand(oldToken.RefreshToken);

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().Be(issued);
        oldToken.IsRevoked.Should().BeTrue();
        user.Tokens.Should().ContainSingle(t => t.AccessToken == issued.AccessToken);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_RefreshTokenIsUnknown()
    {
        // Arrange
        DbSet<UserToken> userTokensMock = MockDbSetHelper.CreateMockDbSet(Array.Empty<UserToken>());
        _dbContext.UserTokens.Returns(userTokensMock);

        var command = new RefreshTokenCommand("unknown-refresh-token");

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_RefreshTokenIsRevoked()
    {
        // Arrange
        User user = CreateUser();
        UserToken oldToken = IssueValidToken(user);
        user.RevokeToken(oldToken.UserTokenId);

        DbSet<UserToken> userTokensMock = MockDbSetHelper.CreateMockDbSet([oldToken]);
        _dbContext.UserTokens.Returns(userTokensMock);

        var command = new RefreshTokenCommand(oldToken.RefreshToken);

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_RefreshTokenIsExpired()
    {
        // Arrange
        User user = CreateUser();
        UserToken oldToken = user.IssueToken(
            "old-access-token",
            "old-refresh-token",
            UtcNow.AddDays(-9),
            UtcNow.AddDays(-3),
            UtcNow.AddDays(-10)).Value;

        DbSet<UserToken> userTokensMock = MockDbSetHelper.CreateMockDbSet([oldToken]);
        _dbContext.UserTokens.Returns(userTokensMock);

        var command = new RefreshTokenCommand(oldToken.RefreshToken);

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_UserIsInactive()
    {
        // Arrange
        User user = CreateUser();
        UserToken oldToken = IssueValidToken(user);
        user.Deactivate(UtcNow);

        DbSet<UserToken> userTokensMock = MockDbSetHelper.CreateMockDbSet([oldToken]);
        _dbContext.UserTokens.Returns(userTokensMock);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        var command = new RefreshTokenCommand(oldToken.RefreshToken);

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnFailure_When_NewTokenExpirationIsInvalid()
    {
        // Arrange — CreateToken returns an access token that already expires at issuance time.
        User user = CreateUser();
        UserToken oldToken = IssueValidToken(user);

        DbSet<UserToken> userTokensMock = MockDbSetHelper.CreateMockDbSet([oldToken]);
        _dbContext.UserTokens.Returns(userTokensMock);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);

        IdentityToken invalidIssued = new(
            AccessToken: "new-access-token",
            AccessTokenExpiresOn: new DateTimeOffset(UtcNow, TimeSpan.Zero),
            RefreshToken: "new-refresh-token",
            RefreshTokenExpiresOn: new DateTimeOffset(UtcNow.AddDays(7), TimeSpan.Zero));
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(invalidIssued);

        var command = new RefreshTokenCommand(oldToken.RefreshToken);

        // Act
        Result<IdentityToken> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserTokenErrors.InvalidExpiration);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PassUserRolesIntoTokenRequest_When_UserHasRoles()
    {
        // Arrange
        User user = CreateUser();
        user.AssignRole(Role.Administrator);
        UserToken oldToken = IssueValidToken(user);

        DbSet<UserToken> userTokensMock = MockDbSetHelper.CreateMockDbSet([oldToken]);
        _dbContext.UserTokens.Returns(userTokensMock);
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet([user]);
        _dbContext.Users.Returns(usersMock);
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(CreateIdentityToken());

        var command = new RefreshTokenCommand(oldToken.RefreshToken);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenProvider.Received(1).CreateToken(Arg.Is<TokenRequest>(r =>
            r.UserId == user.Id &&
            r.Email == user.Email.Value &&
            r.Roles.Contains(Role.Administrator.Name)));
    }

    private static User CreateUser()
    {
        Email email = Email.Create("john.doe@example.com").Value;
        Password password = Password.Create(HashedPassword).Value;
        return User.Create("John", "Doe", email, password, UtcNow).Value;
    }

    private static UserToken IssueValidToken(User user) => user.IssueToken(
        "old-access-token",
        "old-refresh-token",
        UtcNow.AddMinutes(60),
        UtcNow.AddDays(7),
        UtcNow).Value;

    private static IdentityToken CreateIdentityToken() => new(
        AccessToken: "new-access-token",
        AccessTokenExpiresOn: new DateTimeOffset(UtcNow.AddMinutes(60), TimeSpan.Zero),
        RefreshToken: "new-refresh-token",
        RefreshTokenExpiresOn: new DateTimeOffset(UtcNow.AddDays(7), TimeSpan.Zero));
}
