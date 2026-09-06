namespace Aurora.Flowboard.Application.WorkItems.Shared;

internal static class WorkItemAccessExtensions
{
    public static Task<bool> CanAccessWorkItemAsync(
        this IApplicationDbContext dbContext,
        Guid workItemId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.WorkItems
            .AsNoTracking()
            .AnyAsync(w => w.Id == workItemId && w.Project.Members.Any(m => m.UserId == userId), cancellationToken);
}
