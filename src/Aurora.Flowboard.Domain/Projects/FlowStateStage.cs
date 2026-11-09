namespace Aurora.Flowboard.Domain.Projects;

// Where an open work item stands in its project's flow. Not persisted: it follows from the
// project's Active states (FlowState.GetBacklogStateId).
public enum FlowStateStage
{
    Backlog = 0,
    InFlight = 1
}
