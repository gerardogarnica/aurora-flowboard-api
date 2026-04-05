namespace Aurora.Flowboard.Application.Flows.Deactivate;

public sealed record DeactivateFlowCommand(Guid Id) : ICommand;
