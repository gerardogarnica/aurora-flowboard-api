namespace Aurora.Flowboard.Application.UnitTests.Abstractions;

internal static class DbContextSeedExtensions
{
    // Builds the mock set before touching the context property: nesting CreateMockDbSet inside
    // Returns(...) makes NSubstitute throw CouldNotSetReturnDueToNoLastCallException.
    internal static DbSet<T> Seed<T>(
        this IApplicationDbContext dbContext,
        Func<IApplicationDbContext, DbSet<T>> selector,
        params IEnumerable<T> items) where T : class
    {
        DbSet<T> mockSet = MockDbSetHelper.CreateMockDbSet(items);
        selector(dbContext).Returns(mockSet);

        return mockSet;
    }
}
