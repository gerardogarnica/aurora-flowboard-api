namespace Aurora.Flowboard.Application.WorkItems.UpdateType;

internal sealed class UpdateWorkItemTypeValidator : AbstractValidator<UpdateWorkItemTypeCommand>
{
    public UpdateWorkItemTypeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Type)
            .IsInEnum();
    }
}
