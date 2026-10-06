namespace Aurora.Flowboard.Application.Projects.Shared;

// Single source of the "available transitions" contract shared by GET projects/{id}/board (per column)
// and GET work-items/{code} (per item): every destination category, filtered only by the requester's
// role (no Viewer special-casing), ordered by destination name.
internal static class FlowTransitionQueryExtensions
{
    public static async Task<Dictionary<Guid, List<WorkItemFlowTransitionResponse>>> GetAvailableTransitionsByStateAsync(
        this IApplicationDbContext dbContext,
        Guid projectId,
        IReadOnlyCollection<Guid> fromStateIds,
        ProjectRole role,
        IReadOnlyDictionary<Guid, string> stateNames,
        CancellationToken cancellationToken)
    {
        if (fromStateIds.Count == 0)
        {
            return [];
        }

        List<FlowTransition> transitions = await dbContext
            .FlowTransitions
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId && fromStateIds.Contains(t.FromStateId))
            .ToListAsync(cancellationToken);

        // AllowedRoles is not translatable to SQL, so the role filter runs in memory.
        return transitions
            .Where(t => t.AllowedRoles.Contains(role))
            .GroupBy(t => t.FromStateId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .Select(t => new WorkItemFlowTransitionResponse(t.ToStateId, stateNames.GetValueOrDefault(t.ToStateId, string.Empty)))
                    .OrderBy(t => t.ToStateName)
                    .ToList());
    }
}
