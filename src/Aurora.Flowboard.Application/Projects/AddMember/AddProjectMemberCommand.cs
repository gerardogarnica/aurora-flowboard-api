namespace Aurora.Flowboard.Application.Projects.AddMember;

public sealed record AddProjectMemberCommand(
    Guid ProjectId,
    Guid UserId,
    ProjectRole Role) : ICommand;
