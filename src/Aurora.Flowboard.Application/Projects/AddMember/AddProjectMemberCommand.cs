namespace Aurora.Flowboard.Application.Projects.AddProjectMember;

public sealed record AddProjectMemberCommand(
    Guid ProjectId,
    Guid UserId,
    ProjectRole Role) : ICommand;
