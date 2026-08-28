namespace Aurora.Flowboard.Application.TemplateFlows.GetByKind;

internal sealed class GetTemplateFlowByKindHandler(
    IApplicationDbContext dbContext) : IQueryHandler<GetTemplateFlowByKindQuery, TemplateFlowResponse>
{
    public async Task<Result<TemplateFlowResponse>> Handle(
        GetTemplateFlowByKindQuery query,
        CancellationToken cancellationToken)
    {
        TemplateFlow? template = await dbContext
            .TemplateFlows
            .Include(t => t.States)
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Kind == query.Kind, cancellationToken);

        if (template is null)
        {
            return Result.Fail<TemplateFlowResponse>(TemplateFlowErrors.NotFound);
        }

        return new TemplateFlowResponse(
            template.Id,
            template.Kind,
            [.. template.States
                .OrderBy(s => s.Category)
                .ThenBy(s => s.SortOrder)
                .Select(s => new TemplateFlowStateResponse(s.Id, s.Name, s.SortOrder, s.Category, s.Color.Value))]);
    }
}
