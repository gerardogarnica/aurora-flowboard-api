using Aurora.Flowboard.Application.Abstractions.Behaviors;
using Aurora.Flowboard.Application.Abstractions.Messaging;
using Aurora.Flowboard.Application.Authentication.Logout;
using Aurora.Flowboard.Application.Authentication.RefreshToken;
using Aurora.Flowboard.Application.Users.CreateUser;
using Aurora.Flowboard.Application.Users.GetUserById;
using Microsoft.Extensions.Logging;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions.Behaviors;

public sealed class LoggingBehaviorTests
{
    private const string Password = "Sup3r-Secret-Pa$$word";
    private const string RefreshToken = "refresh-token-7f3a9c1e5b";
    private const string IssuedAccessToken = "issued-access-token-a81d4f";
    private const string IssuedRefreshToken = "issued-refresh-token-c62e90";
    private static readonly Guid UserId = new("6f1c2d3e-4b5a-4c7d-8e9f-0a1b2c3d4e5f");

    [Fact]
    public async Task Should_NotLogPassword_When_CommandWithResponseIsHandled()
    {
        // Arrange
        var command = new CreateUserCommand("Ada", "Lovelace", "ada@flowboard.dev", Password, "Member");

        ICommandHandler<CreateUserCommand, Guid> innerHandler = Substitute.For<ICommandHandler<CreateUserCommand, Guid>>();
        innerHandler.Handle(command, Arg.Any<CancellationToken>()).Returns(Result.Ok(UserId));

        var logger = new CapturingLogger<LoggingBehavior.CommandHandler<CreateUserCommand, Guid>>();
        var behavior = new LoggingBehavior.CommandHandler<CreateUserCommand, Guid>(innerHandler, logger);

        // Act
        await behavior.Handle(command, CancellationToken.None);

        // Assert
        logger.Entries.Should().Contain(e => e.Contains(nameof(CreateUserCommand)));
        logger.Entries.Should().NotContain(e => e.Contains(Password));
    }

    [Fact]
    public async Task Should_NotLogRefreshToken_When_CommandWithoutResponseIsHandled()
    {
        // Arrange
        var command = new LogoutCommand(RefreshToken);

        ICommandHandler<LogoutCommand> innerHandler = Substitute.For<ICommandHandler<LogoutCommand>>();
        innerHandler.Handle(command, Arg.Any<CancellationToken>()).Returns(Result.Ok());

        var logger = new CapturingLogger<LoggingBehavior.CommandBaseHandler<LogoutCommand>>();
        var behavior = new LoggingBehavior.CommandBaseHandler<LogoutCommand>(innerHandler, logger);

        // Act
        await behavior.Handle(command, CancellationToken.None);

        // Assert
        logger.Entries.Should().Contain(e => e.Contains(nameof(LogoutCommand)));
        logger.Entries.Should().NotContain(e => e.Contains(RefreshToken));
    }

    [Fact]
    public async Task Should_NotLogRequestPayload_When_QueryIsHandled()
    {
        // Arrange
        var query = new GetUserByIdQuery(UserId);

        IQueryHandler<GetUserByIdQuery, UserProfileResponse> innerHandler =
            Substitute.For<IQueryHandler<GetUserByIdQuery, UserProfileResponse>>();
        innerHandler.Handle(query, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<UserProfileResponse>(UserErrors.NotFound));

        var logger = new CapturingLogger<LoggingBehavior.QueryHandler<GetUserByIdQuery, UserProfileResponse>>();
        var behavior = new LoggingBehavior.QueryHandler<GetUserByIdQuery, UserProfileResponse>(innerHandler, logger);

        // Act
        await behavior.Handle(query, CancellationToken.None);

        // Assert
        logger.Entries.Should().Contain(e => e.Contains(nameof(GetUserByIdQuery)));
        logger.Entries.Should().NotContain(e => e.Contains(UserId.ToString()));
    }

    [Fact]
    public async Task Should_NameQueryInEveryEntry_When_QueryIsHandled()
    {
        // Arrange
        var query = new GetUserByIdQuery(UserId);

        IQueryHandler<GetUserByIdQuery, UserProfileResponse> innerHandler =
            Substitute.For<IQueryHandler<GetUserByIdQuery, UserProfileResponse>>();
        innerHandler.Handle(query, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<UserProfileResponse>(UserErrors.NotFound));

        var logger = new CapturingLogger<LoggingBehavior.QueryHandler<GetUserByIdQuery, UserProfileResponse>>();
        var behavior = new LoggingBehavior.QueryHandler<GetUserByIdQuery, UserProfileResponse>(innerHandler, logger);

        // Act
        await behavior.Handle(query, CancellationToken.None);

        // Assert
        logger.Entries.Should().HaveCount(2)
            .And.OnlyContain(e => e.Contains(nameof(GetUserByIdQuery)));
    }

    [Fact]
    public async Task Should_LogErrorCode_When_CommandWithResponseFails()
    {
        // Arrange
        var command = new CreateUserCommand("Ada", "Lovelace", "ada@flowboard.dev", Password, "Member");

        ICommandHandler<CreateUserCommand, Guid> innerHandler = Substitute.For<ICommandHandler<CreateUserCommand, Guid>>();
        innerHandler.Handle(command, Arg.Any<CancellationToken>()).Returns(Result.Fail<Guid>(UserErrors.NotFound));

        var logger = new CapturingLogger<LoggingBehavior.CommandHandler<CreateUserCommand, Guid>>();
        var behavior = new LoggingBehavior.CommandHandler<CreateUserCommand, Guid>(innerHandler, logger);

        // Act
        await behavior.Handle(command, CancellationToken.None);

        // Assert
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Contains("User.NotFound"));
    }

    [Fact]
    public async Task Should_LogErrorCode_When_CommandWithoutResponseFailsUnexpectedly()
    {
        // Arrange
        var command = new LogoutCommand(RefreshToken);

        ICommandHandler<LogoutCommand> innerHandler = Substitute.For<ICommandHandler<LogoutCommand>>();
        innerHandler.Handle(command, Arg.Any<CancellationToken>())
            .Returns(Result.Fail(BaseError.Failure("Auth.Unexpected", "Unexpected failure while logging out")));

        var logger = new CapturingLogger<LoggingBehavior.CommandBaseHandler<LogoutCommand>>();
        var behavior = new LoggingBehavior.CommandBaseHandler<LogoutCommand>(innerHandler, logger);

        // Act
        await behavior.Handle(command, CancellationToken.None);

        // Assert
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error && e.Contains("Auth.Unexpected"));
    }

    [Fact]
    public async Task Should_LogErrorCode_When_QueryFails()
    {
        // Arrange
        var query = new GetUserByIdQuery(UserId);

        IQueryHandler<GetUserByIdQuery, UserProfileResponse> innerHandler =
            Substitute.For<IQueryHandler<GetUserByIdQuery, UserProfileResponse>>();
        innerHandler.Handle(query, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<UserProfileResponse>(UserErrors.NotFound));

        var logger = new CapturingLogger<LoggingBehavior.QueryHandler<GetUserByIdQuery, UserProfileResponse>>();
        var behavior = new LoggingBehavior.QueryHandler<GetUserByIdQuery, UserProfileResponse>(innerHandler, logger);

        // Act
        await behavior.Handle(query, CancellationToken.None);

        // Assert
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Contains("User.NotFound"));
    }

    [Fact]
    public async Task Should_NotLogIssuedTokens_When_CommandReturnsIdentityToken()
    {
        // Arrange
        var command = new RefreshTokenCommand(RefreshToken);
        var issued = new IdentityToken(
            IssuedAccessToken,
            new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero),
            IssuedRefreshToken,
            new DateTimeOffset(2026, 1, 8, 0, 0, 0, TimeSpan.Zero));

        ICommandHandler<RefreshTokenCommand, IdentityToken> innerHandler =
            Substitute.For<ICommandHandler<RefreshTokenCommand, IdentityToken>>();
        innerHandler.Handle(command, Arg.Any<CancellationToken>()).Returns(Result.Ok(issued));

        var logger = new CapturingLogger<LoggingBehavior.CommandHandler<RefreshTokenCommand, IdentityToken>>();
        var behavior = new LoggingBehavior.CommandHandler<RefreshTokenCommand, IdentityToken>(innerHandler, logger);

        // Act
        await behavior.Handle(command, CancellationToken.None);

        // Assert
        logger.Entries.Should().HaveCount(2);
        logger.Entries.Should().NotContain(e => e.Contains(IssuedAccessToken) || e.Contains(IssuedRefreshToken));
    }
}
