namespace Aurora.Flowboard.Application.Projects.Shared;

internal static class ProjectMembershipExtensions
{
    public static Task<bool> IsProjectMemberAsync(
        this IApplicationDbContext dbContext,
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.Members.Any(m => m.UserId == userId), cancellationToken);
}
