namespace Aurora.Flowboard.Application.WorkItems.ChangeComponent;

internal sealed class ChangeWorkItemComponentValidator : AbstractValidator<ChangeWorkItemComponentCommand>
{
    public ChangeWorkItemComponentValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
