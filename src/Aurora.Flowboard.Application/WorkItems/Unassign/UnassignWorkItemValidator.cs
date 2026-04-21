namespace Aurora.Flowboard.Application.WorkItems.Unassign;

internal sealed class UnassignWorkItemValidator : AbstractValidator<UnassignWorkItemCommand>
{
    public UnassignWorkItemValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
