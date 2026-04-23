namespace Aurora.Flowboard.Application.WorkItems.AddComment;

internal sealed class AddWorkItemCommentValidator : AbstractValidator<AddWorkItemCommentCommand>
{
    public AddWorkItemCommentValidator()
    {
        RuleFor(x => x.WorkItemId)
            .NotEmpty();

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(Comment.MaxContentLength);
    }
}
