namespace Aurora.Flowboard.Application.WorkItems.Shared;

internal static class OpenWorkItemQueryExtensions
{
    // Open means not in a terminal state: closing a milestone or retiring a component waits for these.
    public static Task<int> CountOpenAsync(
        this IQueryable<WorkItem> workItems,
        CancellationToken cancellationToken) =>
        workItems
            .Where(w => w.FlowState.Category != FlowStateCategory.Completed
                && w.FlowState.Category != FlowStateCategory.Cancelled)
            .CountAsync(cancellationToken);
}
