namespace Aurora.Flowboard.Infrastructure.Database;

internal sealed class ApplicationDbContextFactory(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IApplicationDbContextFactory
{
    public async Task<IApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        await dbContextFactory.CreateDbContextAsync(cancellationToken);
}
