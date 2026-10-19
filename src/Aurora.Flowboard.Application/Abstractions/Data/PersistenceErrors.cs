namespace Aurora.Flowboard.Application.Abstractions.Data;

public static class PersistenceErrors
{
    // Postgres SqlStates, read through System.Data.Common.DbException so Application does not depend on Npgsql.
    public const string UniqueViolationSqlState = "23505";
    public const string SerializationFailureSqlState = "40001";
    public const string DeadlockDetectedSqlState = "40P01";

    public static readonly BaseError ConcurrencyConflict = BaseError.Conflict(
        "Persistence.ConcurrencyConflict",
        "The resource was modified by another request. Reload it and try again.");

    public static readonly BaseError UniqueViolation = BaseError.Conflict(
        "Persistence.UniqueViolation",
        "A resource with the same unique value already exists.");
}
