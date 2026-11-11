namespace Aurora.Flowboard.Application.Projects.Shared;

// A Completed or Archived project is reached through GET projects, not through these screens.
internal static class MyProjectsQueryExtensions
{
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
