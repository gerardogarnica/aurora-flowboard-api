namespace Aurora.Flowboard.Application.Flows.AddState;

public sealed record AddFlowStateCommand(
    Guid FlowId,
    string Name,
    int SortOrder,
    bool IsTerminal) : ICommand;
