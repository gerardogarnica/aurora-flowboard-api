namespace Aurora.Flowboard.Application.WorkItems.RemoveComment;

internal sealed class RemoveWorkItemCommentValidator : AbstractValidator<RemoveWorkItemCommentCommand>
{
    public RemoveWorkItemCommentValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();

        RuleFor(x => x.CommentId)
            .NotEmpty();
    }
}
