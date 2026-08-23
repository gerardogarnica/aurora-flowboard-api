namespace Aurora.Flowboard.Application.WorkItems.UpdateDescription;

public sealed record UpdateWorkItemDescriptionCommand(Guid Id, string? Description) : ICommand;
