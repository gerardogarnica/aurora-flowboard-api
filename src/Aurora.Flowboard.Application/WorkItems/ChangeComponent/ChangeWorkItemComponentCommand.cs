namespace Aurora.Flowboard.Application.WorkItems.ChangeComponent;

public sealed record ChangeWorkItemComponentCommand(Guid Id, Guid? ComponentId) : ICommand;
