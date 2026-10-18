using Aurora.Flowboard.Application.Abstractions.Behaviors;
using Aurora.Flowboard.Application.Abstractions.Messaging;
using Aurora.Flowboard.Application.UnitTests.Milestones;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions.Behaviors;

public sealed class PersistenceConflictBehaviorTests
{
    private const string ForeignKeyViolationSqlState = "23503";
    private static readonly Guid MilestoneId = new("3b2f6a1c-8d4e-4f5a-9b7c-1e2d3c4b5a69");
    private static readonly Guid ProjectId = new("7c1d2e3f-4a5b-4c6d-8e7f-9a0b1c2d3e4f");

    private readonly ChangeMilestoneStatusCommand _command =
        MilestoneCommandData.GetChangeStatusCommand(MilestoneId, MilestoneStatus.Active);
    private readonly ICommandHandler<ChangeMilestoneStatusCommand> _innerHandler =
        Substitute.For<ICommandHandler<ChangeMilestoneStatusCommand>>();
    private readonly CapturingLogger<PersistenceConflictBehavior.CommandBaseHandler<ChangeMilestoneStatusCommand>> _logger = new();
    private readonly PersistenceConflictBehavior.CommandBaseHandler<ChangeMilestoneStatusCommand> _behavior;

    private readonly CreateMilestoneCommand _commandWithResponse = MilestoneCommandData.GetCreateCommand(ProjectId);
    private readonly ICommandHandler<CreateMilestoneCommand, Guid> _innerHandlerWithResponse =
        Substitute.For<ICommandHandler<CreateMilestoneCommand, Guid>>();
    private readonly PersistenceConflictBehavior.CommandHandler<CreateMilestoneCommand, Guid> _behaviorWithResponse;

    public PersistenceConflictBehaviorTests()
    {
        _behavior = new PersistenceConflictBehavior.CommandBaseHandler<ChangeMilestoneStatusCommand>(_innerHandler, _logger);
        _behaviorWithResponse = new PersistenceConflictBehavior.CommandHandler<CreateMilestoneCommand, Guid>(
            _innerHandlerWithResponse,
            new CapturingLogger<PersistenceConflictBehavior.CommandHandler<CreateMilestoneCommand, Guid>>());
    }

    [Fact]
    public async Task Should_ReturnConcurrencyConflict_When_HandlerThrowsDbUpdateConcurrencyException()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("milestones row changed"));

        // Act
        Result result = await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.ConcurrencyConflict);
        result.Error.ErrorType.Should().Be(BaseErrorType.Conflict);
    }

    [Fact]
    public async Task Should_ReturnConcurrencyConflict_When_QueryOrCommitThrowsSerializationFailure()
    {
        // Arrange — a serialization failure raised by a query or CommitAsync is not wrapped.
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new FakeDbException(PersistenceErrors.SerializationFailureSqlState));

        // Act
        Result result = await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.ConcurrencyConflict);
    }

    [Fact]
    public async Task Should_ReturnConcurrencyConflict_When_SaveChangesWrapsSerializationFailure()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException(
                "save failed",
                new FakeDbException(PersistenceErrors.SerializationFailureSqlState)));

        // Act
        Result result = await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.ConcurrencyConflict);
    }

    [Fact]
    public async Task Should_ReturnConcurrencyConflict_When_QueryOrCommitThrowsDeadlock()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new FakeDbException(PersistenceErrors.DeadlockDetectedSqlState));

        // Act
        Result result = await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.ConcurrencyConflict);
    }

    [Fact]
    public async Task Should_ReturnConcurrencyConflict_When_SaveChangesWrapsDeadlock()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException(
                "save failed",
                new FakeDbException(PersistenceErrors.DeadlockDetectedSqlState)));

        // Act
        Result result = await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.ConcurrencyConflict);
    }

    [Fact]
    public async Task Should_ReturnUniqueViolation_When_SaveChangesWrapsUniqueViolation()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException(
                "save failed",
                new FakeDbException(PersistenceErrors.UniqueViolationSqlState)));

        // Act
        Result result = await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.UniqueViolation);
        result.Error.ErrorType.Should().Be(BaseErrorType.Conflict);
    }

    [Fact]
    public async Task Should_Rethrow_When_DbUpdateExceptionHasAnotherSqlState()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException("save failed", new FakeDbException(ForeignKeyViolationSqlState)));

        // Act
        Func<Task> act = () => _behavior.Handle(_command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Should_Rethrow_When_HandlerThrowsAnotherException()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("unexpected"));

        // Act
        Func<Task> act = () => _behavior.Handle(_command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_Rethrow_When_HandlerIsCancelled()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        Func<Task> act = () => _behavior.Handle(_command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Should_ReturnHandlerResult_When_HandlerDoesNotThrow()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .Returns(Result.Fail(MilestoneErrors.NotFound));

        // Act
        Result result = await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(MilestoneErrors.NotFound);
    }

    [Fact]
    public async Task Should_LogWarning_When_ConflictIsTranslated()
    {
        // Arrange
        _innerHandler.Handle(_command, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("milestones row changed"));

        // Act
        await _behavior.Handle(_command, CancellationToken.None);

        // Assert
        _logger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Warning
            && e.Contains(nameof(ChangeMilestoneStatusCommand))
            && e.Contains(PersistenceErrors.ConcurrencyConflict.Code));
    }

    [Fact]
    public async Task Should_ReturnConcurrencyConflict_When_HandlerWithResponseThrowsDbUpdateConcurrencyException()
    {
        // Arrange
        _innerHandlerWithResponse.Handle(_commandWithResponse, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("projects row changed"));

        // Act
        Result<Guid> result = await _behaviorWithResponse.Handle(_commandWithResponse, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.ConcurrencyConflict);
    }

    [Fact]
    public async Task Should_ReturnUniqueViolation_When_HandlerWithResponseThrowsUniqueViolation()
    {
        // Arrange
        _innerHandlerWithResponse.Handle(_commandWithResponse, Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateException(
                "save failed",
                new FakeDbException(PersistenceErrors.UniqueViolationSqlState)));

        // Act
        Result<Guid> result = await _behaviorWithResponse.Handle(_commandWithResponse, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(PersistenceErrors.UniqueViolation);
    }

    [Fact]
    public async Task Should_ReturnHandlerValue_When_HandlerWithResponseSucceeds()
    {
        // Arrange
        _innerHandlerWithResponse.Handle(_commandWithResponse, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(MilestoneId));

        // Act
        Result<Guid> result = await _behaviorWithResponse.Handle(_commandWithResponse, CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Value.Should().Be(MilestoneId);
    }
}
