namespace Aurora.Flowboard.Application.TemplateFlows.GetByKind;

internal sealed class GetTemplateFlowByKindValidator : AbstractValidator<GetTemplateFlowByKindQuery>
{
    public GetTemplateFlowByKindValidator()
    {
        RuleFor(x => x.Kind)
            .IsInEnum();
    }
}
