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

    // Membership check and role lookup in one round trip: null means the user is not a member.
    public static Task<ProjectRole?> GetProjectMemberRoleAsync(
        this IApplicationDbContext dbContext,
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .SelectMany(p => p.Members)
            .Where(m => m.UserId == userId)
            .Select(m => (ProjectRole?)m.Role)
            .FirstOrDefaultAsync(cancellationToken);
}
