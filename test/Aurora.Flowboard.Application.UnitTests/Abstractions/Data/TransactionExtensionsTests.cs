using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions.Data;

public sealed class TransactionExtensionsTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IDbContextTransaction _transaction = Substitute.For<IDbContextTransaction>();

    public TransactionExtensionsTests()
    {
        _dbContext.BeginTransactionAsync(Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>())
            .Returns(_transaction);
    }

    [Fact]
    public async Task Should_RunOperationWithoutTransaction_When_IsolationLevelIsNull()
    {
        // Arrange
        bool operationRan = false;

        // Act
        Result result = await _dbContext.ExecuteInTransactionAsync(
            null,
            _ =>
            {
                operationRan = true;
                return Task.FromResult(Result.Ok());
            },
            CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        operationRan.Should().BeTrue();
        await _dbContext.DidNotReceive()
            .BeginTransactionAsync(Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnOperationError_When_IsolationLevelIsNullAndOperationFails()
    {
        // Act
        Result result = await _dbContext.ExecuteInTransactionAsync(
            null,
            _ => Task.FromResult(Result.Fail(UserErrors.NotFound)),
            CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Should_OpenTransactionWithIsolationLevel_When_IsolationLevelIsGiven()
    {
        // Act
        await _dbContext.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            _ => Task.FromResult(Result.Ok()),
            CancellationToken.None);

        // Assert
        await _dbContext.Received(1)
            .BeginTransactionAsync(IsolationLevel.Serializable, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Commit_When_OperationSucceeds()
    {
        // Act
        Result result = await _dbContext.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            _ => Task.FromResult(Result.Ok()),
            CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotCommit_When_OperationFails()
    {
        // Act
        Result result = await _dbContext.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            _ => Task.FromResult(Result.Fail(UserErrors.NotFound)),
            CancellationToken.None);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound);
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_DisposeTransaction_When_OperationCompletes()
    {
        // Act
        await _dbContext.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            _ => Task.FromResult(Result.Ok()),
            CancellationToken.None);

        // Assert
        await _transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Should_DisposeTransaction_When_OperationFails()
    {
        // Act
        await _dbContext.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            _ => Task.FromResult(Result.Fail(UserErrors.NotFound)),
            CancellationToken.None);

        // Assert
        await _transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Should_DisposeWithoutCommit_When_OperationThrows()
    {
        // Act
        Func<Task> act = () => _dbContext.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            _ => throw new InvalidOperationException("unexpected"),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Should_PassCancellationToken_When_IsolationLevelIsGiven()
    {
        // Arrange
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        CancellationToken operationToken = default;

        // Act
        await _dbContext.ExecuteInTransactionAsync(
            IsolationLevel.Serializable,
            ct =>
            {
                operationToken = ct;
                return Task.FromResult(Result.Ok());
            },
            cancellationToken);

        // Assert
        operationToken.Should().Be(cancellationToken);
        await _dbContext.Received(1).BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await _transaction.Received(1).CommitAsync(cancellationToken);
    }

    [Fact]
    public async Task Should_PassCancellationToken_When_IsolationLevelIsNull()
    {
        // Arrange
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        CancellationToken operationToken = default;

        // Act
        await _dbContext.ExecuteInTransactionAsync(
            null,
            ct =>
            {
                operationToken = ct;
                return Task.FromResult(Result.Ok());
            },
            cancellationToken);

        // Assert
        operationToken.Should().Be(cancellationToken);
    }
}
