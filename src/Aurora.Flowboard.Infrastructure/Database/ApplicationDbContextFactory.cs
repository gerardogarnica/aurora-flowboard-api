namespace Aurora.Flowboard.Infrastructure.Database;

internal sealed class ApplicationDbContextFactory(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IApplicationDbContextFactory
{
    public async Task<IApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await dbContextFactory.CreateDbContextAsync(cancellationToken);
    }
}
