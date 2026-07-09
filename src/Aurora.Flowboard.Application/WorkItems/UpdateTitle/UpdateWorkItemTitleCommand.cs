namespace Aurora.Flowboard.Application.WorkItems.UpdateTitle;

public sealed record UpdateWorkItemTitleCommand(Guid Id, string Title) : ICommand;
