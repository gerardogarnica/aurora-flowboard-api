using Aurora.Flowboard.Application.Abstractions.Behaviors;
using Aurora.Flowboard.Application.Abstractions.Messaging;
using Aurora.Flowboard.Application.Authentication.Logout;
using Aurora.Flowboard.Application.Authentication.RefreshToken;
using Aurora.Flowboard.Application.Users.GetUserById;
using Microsoft.Extensions.Logging;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions.Behaviors;

public sealed class PerformanceBehaviorTests
{
    // Above the behavior's 500 ms threshold, so the long-running warning is written.
    private const int SlowHandlerDelayMilliseconds = 600;
    private const string RefreshToken = "refresh-token-7f3a9c1e5b";
    private static readonly Guid UserId = new("6f1c2d3e-4b5a-4c7d-8e9f-0a1b2c3d4e5f");

    [Fact]
    public async Task Should_NotLogRefreshToken_When_SlowCommandWithResponseIsHandled()
    {
        // Arrange
        var command = new RefreshTokenCommand(RefreshToken);

        ICommandHandler<RefreshTokenCommand, IdentityToken> innerHandler =
            Substitute.For<ICommandHandler<RefreshTokenCommand, IdentityToken>>();
        innerHandler.Handle(command, Arg.Any<CancellationToken>())
            .Returns(_ => DelayedAsync(Result.Fail<IdentityToken>(UserErrors.NotFound)));

        var logger = new CapturingLogger<PerformanceBehavior.CommandHandler<RefreshTokenCommand, IdentityToken>>();
        var behavior = new PerformanceBehavior.CommandHandler<RefreshTokenCommand, IdentityToken>(innerHandler, logger);

        // Act
        await behavior.Handle(command, CancellationToken.None);

        // Assert
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Contains(nameof(RefreshTokenCommand)));
        logger.Entries.Should().NotContain(e => e.Contains(RefreshToken));
    }

    [Fact]
    public async Task Should_NotLogRefreshToken_When_SlowCommandWithoutResponseIsHandled()
    {
        // Arrange
        var command = new LogoutCommand(RefreshToken);

        ICommandHandler<LogoutCommand> innerHandler = Substitute.For<ICommandHandler<LogoutCommand>>();
        innerHandler.Handle(command, Arg.Any<CancellationToken>())
            .Returns(_ => DelayedAsync(Result.Ok()));

        var logger = new CapturingLogger<PerformanceBehavior.CommandBaseHandler<LogoutCommand>>();
        var behavior = new PerformanceBehavior.CommandBaseHandler<LogoutCommand>(innerHandler, logger);

        // Act
        await behavior.Handle(command, CancellationToken.None);

        // Assert
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Contains(nameof(LogoutCommand)));
        logger.Entries.Should().NotContain(e => e.Contains(RefreshToken));
    }

    [Fact]
    public async Task Should_NotLogRequestPayload_When_SlowQueryIsHandled()
    {
        // Arrange
        var query = new GetUserByIdQuery(UserId);

        IQueryHandler<GetUserByIdQuery, UserProfileResponse> innerHandler =
            Substitute.For<IQueryHandler<GetUserByIdQuery, UserProfileResponse>>();
        innerHandler.Handle(query, Arg.Any<CancellationToken>())
            .Returns(_ => DelayedAsync(Result.Fail<UserProfileResponse>(UserErrors.NotFound)));

        var logger = new CapturingLogger<PerformanceBehavior.QueryHandler<GetUserByIdQuery, UserProfileResponse>>();
        var behavior = new PerformanceBehavior.QueryHandler<GetUserByIdQuery, UserProfileResponse>(innerHandler, logger);

        // Act
        await behavior.Handle(query, CancellationToken.None);

        // Assert
        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Contains(nameof(GetUserByIdQuery)));
        logger.Entries.Should().NotContain(e => e.Contains(UserId.ToString()));
    }

    private static async Task<TResult> DelayedAsync<TResult>(TResult result)
    {
        await Task.Delay(SlowHandlerDelayMilliseconds);

        return result;
    }
}
