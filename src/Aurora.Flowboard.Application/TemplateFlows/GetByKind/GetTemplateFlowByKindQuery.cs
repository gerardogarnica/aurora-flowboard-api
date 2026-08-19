namespace Aurora.Flowboard.Application.TemplateFlows.GetByKind;

public sealed record GetTemplateFlowByKindQuery(ProjectKind Kind) : IQuery<TemplateFlowResponse>;
