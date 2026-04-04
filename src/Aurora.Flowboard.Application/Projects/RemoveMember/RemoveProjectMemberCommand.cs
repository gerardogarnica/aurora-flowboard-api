namespace Aurora.Flowboard.Application.Projects.RemoveMember;

public sealed record RemoveProjectMemberCommand(
    Guid ProjectId,
    Guid UserId) : ICommand;
