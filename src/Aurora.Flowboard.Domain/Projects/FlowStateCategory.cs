namespace Aurora.Flowboard.Domain.Projects;

public enum FlowStateCategory
{
    // Visible on board
    Active = 0,
    // Not visible on board, the last state in the flow, used for archive or close work items
    Completed = 1,
    // Not visible on board, used for remove or delete work items
    Cancelled = 2
}
