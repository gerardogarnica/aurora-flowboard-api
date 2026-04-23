namespace Aurora.Flowboard.Application.WorkItems.Move;

internal sealed class MoveWorkItemValidator : AbstractValidator<MoveWorkItemCommand>
{
    public MoveWorkItemValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.ToStateId)
            .NotEmpty();

        RuleFor(x => x.Reason)
            .MaximumLength(StateTransitionHistory.MaxReasonLength)
            .When(x => x.Reason is not null);
    }
}
