namespace Aurora.Flowboard.Application.Projects.Shared;

internal static class FlowStateStageResolver
{
    // The backlog state of every project in the list, from its Active states (FlowState.GetBacklogStateId).
    public static HashSet<Guid> GetBacklogStateIds(IEnumerable<(Guid Id, Guid ProjectId, int SortOrder)> activeStates) =>
        [.. activeStates
            .GroupBy(s => s.ProjectId)
            .Select(project => FlowState.GetBacklogStateId([.. project.Select(s => (s.Id, s.SortOrder))]))
            .OfType<Guid>()];
}
