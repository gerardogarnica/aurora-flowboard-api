namespace Aurora.Flowboard.Application.WorkItems.Assign;

internal sealed class AssignWorkItemValidator : AbstractValidator<AssignWorkItemCommand>
{
    public AssignWorkItemValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.AssigneeId)
            .NotEmpty();

        RuleFor(x => x.ChangedById)
            .NotEmpty();
    }
}
