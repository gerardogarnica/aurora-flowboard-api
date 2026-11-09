namespace Aurora.Flowboard.Application.Projects.Shared;

// The two project sets of the Home and My Issues (docs/specs/2026-11-07-dashboard-design.md, section 2).
// "My projects" feed the Sidebar, the portfolio rows and the member count. "My active projects" feed
// every open-work number, because the work items of a Completed project can no longer be moved.
internal static class MyProjectsQueryExtensions
{
    public static IQueryable<Project> WhereMyProjects(this IQueryable<Project> projects, Guid userId) =>
        projects.Where(p => p.Status != ProjectStatus.Archived && p.Members.Any(m => m.UserId == userId));

    // The same rule as AcceptsOpenWork, written out because a method call cannot be translated to SQL.
    public static IQueryable<Project> WhereMyActiveProjects(this IQueryable<Project> projects, Guid userId) =>
        projects.Where(p => (p.Status == ProjectStatus.Active || p.Status == ProjectStatus.Maintenance)
            && p.Members.Any(m => m.UserId == userId));

    public static bool AcceptsOpenWork(ProjectStatus status) =>
        status is ProjectStatus.Active or ProjectStatus.Maintenance;

    // Sidebar order: status, then name. Applied in memory: OrdinalIgnoreCase has no SQL translation.
    public static IOrderedEnumerable<T> OrderLikeSidebar<T>(
        this IEnumerable<T> projects,
        Func<T, ProjectStatus> status,
        Func<T, string> name) =>
        projects.OrderBy(status).ThenBy(name, StringComparer.OrdinalIgnoreCase);
}
