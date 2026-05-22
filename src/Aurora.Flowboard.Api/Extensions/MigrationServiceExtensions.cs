using Aurora.Flowboard.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Aurora.Flowboard.Api.Extensions;

internal static class MigrationServiceExtensions
{
    internal static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
