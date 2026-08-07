namespace Aurora.Flowboard.Application.Projects.ChangeKind;

public sealed record ChangeProjectKindCommand(Guid Id, ProjectKind NewKind) : ICommand;
