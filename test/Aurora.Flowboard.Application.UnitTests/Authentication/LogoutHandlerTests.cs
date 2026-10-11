using Aurora.Flowboard.Application.Authentication.Logout;
using NSubstitute.ExceptionExtensions;

namespace Aurora.Flowboard.Application.UnitTests.Authentication;

public sealed class LogoutHandlerTests
{
    private const string HashedPassword = "hashed_password_123";
    private const string PresentedRefreshToken = "presented-refresh-token";
    private const string PresentedRefreshTokenHash = "presented-refresh-token-hash";
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IApplicationDbContext _dbContext;
    private readonly ITokenProvider _tokenProvider;
    private readonly IUserContext _userContext;
    private readonly LogoutHandler _handler;

    public LogoutHandlerTests()
    {
        _dbContext = Substitute.For<IApplicationDbContext>();
        _tokenProvider = Substitute.For<ITokenProvider>();
        _userContext = Substitute.For<IUserContext>();

        _handler = new LogoutHandler(_dbContext, _tokenProvider, _userContext);

        _tokenProvider.HashRefreshToken(PresentedRefreshToken).Returns(PresentedRefreshTokenHash);
    }

    [Fact]
    public async Task Should_RevokeToken_When_RefreshTokenBelongsToCurrentUser()
    {
        // Arrange
        User user = CreateUser();
        UserToken token = IssueToken(user);
        SetupUsers(user);
        _userContext.UserId.Returns(user.Id);

        // Act
        Result result = await _handler.Handle(new LogoutCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_RefreshTokenIsUnknown()
    {
        // Arrange
        User user = CreateUser();
        user.IssueToken("other-access-token-id", "other-refresh-token-hash", UtcNow.AddMinutes(60), UtcNow.AddDays(7), UtcNow);
        SetupUsers(user);
        _userContext.UserId.Returns(user.Id);

        // Act
        Result result = await _handler.Handle(new LogoutCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_RefreshTokenBelongsToAnotherUser()
    {
        // Arrange
        User user = CreateUser();
        UserToken token = IssueToken(user);
        SetupUsers(user);
        _userContext.UserId.Returns(Guid.NewGuid());

        // Act
        Result result = await _handler.Handle(new LogoutCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        token.IsRevoked.Should().BeFalse();
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnSuccessWithoutSaving_When_TokenIsAlreadyRevoked()
    {
        // Arrange
        User user = CreateUser();
        UserToken token = IssueToken(user);
        user.RevokeToken(token.UserTokenId);
        SetupUsers(user);
        _userContext.UserId.Returns(user.Id);

        // Act
        Result result = await _handler.Handle(new LogoutCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_RevokeToken_When_UserIsInactive()
    {
        // Arrange
        User user = CreateUser();
        UserToken token = IssueToken(user);
        user.Deactivate(UtcNow);
        SetupUsers(user);
        _userContext.UserId.Returns(user.Id);

        // Act
        Result result = await _handler.Handle(new LogoutCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_TokenIsRevokedConcurrently()
    {
        // Arrange — a concurrent refresh or logout changed the row first; the session is closed either way.
        User user = CreateUser();
        IssueToken(user);
        SetupUsers(user);
        _userContext.UserId.Returns(user.Id);
        _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("user_tokens row changed"));

        // Act
        Result result = await _handler.Handle(new LogoutCommand(PresentedRefreshToken), CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
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

    private static UserToken IssueToken(User user) => user.IssueToken(
        "access-token-id",
        PresentedRefreshTokenHash,
        UtcNow.AddMinutes(60),
        UtcNow.AddDays(7),
        UtcNow).Value;
}
