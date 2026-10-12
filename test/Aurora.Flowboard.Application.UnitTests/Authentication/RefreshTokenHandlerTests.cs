using Aurora.Flowboard.Application.Authentication;
using Aurora.Flowboard.Application.Authentication.RefreshToken;
using NSubstitute.ExceptionExtensions;

namespace Aurora.Flowboard.Application.UnitTests.Authentication;

public sealed class RefreshTokenHandlerTests
{
    private const string HashedPassword = "hashed_password_123";
    private const string PresentedRefreshToken = "presented-refresh-token";
    private const string PresentedRefreshTokenHash = "presented-refresh-token-hash";
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
        _tokenProvider.HashRefreshToken(PresentedRefreshToken).Returns(PresentedRefreshTokenHash);
    }

    [Fact]
    public async Task Should_ReturnNewIdentityToken_When_RefreshTokenIsValid()
    {
        // Arrange — the stored token only holds the hash, so a match proves the handler hashed the input.
        User user = CreateUser();
        UserToken oldToken = IssueValidToken(user);
        SetupUsers(user);

        IssuedToken issued = CreateIssuedToken();
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(issued);

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().Be(issued.Identity);
        oldToken.IsRevoked.Should().BeTrue();
        user.Tokens.Should().ContainSingle(t =>
            t.AccessTokenId == issued.AccessTokenId &&
            t.RefreshTokenHash == issued.RefreshTokenHash &&
            !t.IsRevoked);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_RefreshTokenIsUnknown()
    {
        // Arrange — the user has a token, but not the presented one.
        User user = CreateUser();
        user.IssueToken("other-access-token-id", "other-refresh-token-hash", UtcNow.AddMinutes(60), UtcNow.AddDays(7), UtcNow);
        SetupUsers(user);

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        result.Error.ErrorType.Should().Be(BaseErrorType.Unauthorized);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_RefreshTokenIsReplayed()
    {
        // Arrange — the token was already rotated by an earlier refresh.
        User user = CreateUser();
        UserToken oldToken = IssueValidToken(user);
        user.RevokeToken(oldToken.UserTokenId);
        SetupUsers(user);

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        _tokenProvider.DidNotReceive().CreateToken(Arg.Any<TokenRequest>());
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_RefreshTokenIsExpired()
    {
        // Arrange
        User user = CreateUser();
        user.IssueToken(
            "old-access-token-id",
            PresentedRefreshTokenHash,
            UtcNow.AddDays(-9),
            UtcNow.AddDays(-3),
            UtcNow.AddDays(-10));
        SetupUsers(user);

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

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
        IssueValidToken(user);
        user.Deactivate(UtcNow);
        SetupUsers(user);

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnInvalidRefreshToken_When_TokenIsRedeemedConcurrently()
    {
        // Arrange — another request revoked the same row first, so the xmin check fails on save.
        User user = CreateUser();
        IssueValidToken(user);
        SetupUsers(user);
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(CreateIssuedToken());
        _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("user_tokens row changed"));

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_NewTokenExpirationIsInvalid()
    {
        // Arrange — CreateToken returns an access token that already expires at issuance time.
        User user = CreateUser();
        IssueValidToken(user);
        SetupUsers(user);
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(CreateIssuedToken(accessTokenExpiresOnUtc: UtcNow));

        // Act
        Result<IdentityToken> result = await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

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
        IssueValidToken(user);
        SetupUsers(user);
        _tokenProvider.CreateToken(Arg.Any<TokenRequest>()).Returns(CreateIssuedToken());

        // Act
        await _handler.Handle(new RefreshTokenCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        _tokenProvider.Received(1).CreateToken(Arg.Is<TokenRequest>(r =>
            r.UserId == user.Id &&
            r.Email == user.Email.Value &&
            r.Roles.Contains(Role.Administrator.Name)));
    }

    private void SetupUsers(params User[] users)
    {
        DbSet<User> usersMock = MockDbSetHelper.CreateMockDbSet(users);
        _dbContext.Users.Returns(usersMock);
    }

    private static User CreateUser()
    {
        Email email = Email.Create("john.doe@example.com").Value;
        Password password = Password.Create(HashedPassword).Value;
        return User.Create("John", "Doe", email, password, UtcNow).Value;
    }

    private static UserToken IssueValidToken(User user) => user.IssueToken(
        "old-access-token-id",
        PresentedRefreshTokenHash,
        UtcNow.AddMinutes(60),
        UtcNow.AddDays(7),
        UtcNow).Value;

    private static IssuedToken CreateIssuedToken(DateTime? accessTokenExpiresOnUtc = null) => new(
        new IdentityToken(
            AccessToken: "new-access-token",
            AccessTokenExpiresOn: new DateTimeOffset(accessTokenExpiresOnUtc ?? UtcNow.AddMinutes(60), TimeSpan.Zero),
            RefreshToken: "new-refresh-token",
            RefreshTokenExpiresOn: new DateTimeOffset(UtcNow.AddDays(7), TimeSpan.Zero)),
        AccessTokenId: "new-access-token-id",
        RefreshTokenHash: "new-refresh-token-hash");
}
