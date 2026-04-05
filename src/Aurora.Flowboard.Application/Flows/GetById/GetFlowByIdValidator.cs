namespace Aurora.Flowboard.Application.Flows.GetById;

internal sealed class GetFlowByIdValidator : AbstractValidator<GetFlowByIdQuery>
{
    public GetFlowByIdValidator()
    {
        RuleFor(x => x.FlowId)
            .NotEmpty();
    }
}
