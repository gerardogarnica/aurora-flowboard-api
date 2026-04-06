namespace Aurora.Flowboard.Application.WorkItems.UpdateComment;

internal sealed class UpdateWorkItemCommentValidator : AbstractValidator<UpdateWorkItemCommentCommand>
{
    public UpdateWorkItemCommentValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();

        RuleFor(x => x.CommentId)
            .NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(4000);
    }
}
