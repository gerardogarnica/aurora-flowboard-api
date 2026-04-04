namespace Aurora.Flowboard.Application.Projects.RemoveProjectMember;

public sealed record RemoveProjectMemberCommand(
    Guid ProjectId,
    Guid UserId) : ICommand;
