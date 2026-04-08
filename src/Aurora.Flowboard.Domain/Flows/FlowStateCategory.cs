namespace Aurora.Flowboard.Domain.Flows;

public enum FlowStateCategory
{
    // Visible on board
    Active,
    // Visible on board, the last state in the flow
    Completed,
    // Not visible on board, but work items can be moved to this state,
    // can be used for archiving or closed states, can't be initial states in transitions
    Terminal
}