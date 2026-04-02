namespace Aurora.Flowboard.Application.Abstractions.Data;

public interface IApplicationDbContextFactory
{
    Task<IApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}
