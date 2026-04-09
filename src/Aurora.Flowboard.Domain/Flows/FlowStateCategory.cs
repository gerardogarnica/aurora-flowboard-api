namespace Aurora.Flowboard.Domain.Flows;

public enum FlowStateCategory
{
    // Visible on board
    Active,
    // Not visible on board, the last state in the flow, used for archive or close work items
    Completed,
    // Not visible on board, used for remove or delete work items
    Cancelled
}