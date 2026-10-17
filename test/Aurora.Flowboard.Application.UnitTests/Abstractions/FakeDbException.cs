using System.Data.Common;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions;

// DbException is abstract, and Npgsql's PostgresException is not referenced by Application tests.
public sealed class FakeDbException(string sqlState) : DbException("Simulated database error")
{
    public override string SqlState { get; } = sqlState;
}
