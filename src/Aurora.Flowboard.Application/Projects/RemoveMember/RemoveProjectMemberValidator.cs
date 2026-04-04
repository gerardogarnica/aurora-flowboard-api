namespace Aurora.Flowboard.Application.Projects.RemoveProjectMember;

internal sealed class RemoveProjectMemberValidator : AbstractValidator<RemoveProjectMemberCommand>
{
    public RemoveProjectMemberValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty();

        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
