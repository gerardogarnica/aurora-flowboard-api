using Aurora.Flowboard.Application.TemplateFlows.GetByKind;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Api.Endpoints.TemplateFlows;

public sealed class GetTemplateFlowByKind : IBaseEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "template-flows/{kind}",
            async (
                ProjectKind kind,
                IQueryHandler<GetTemplateFlowByKindQuery, TemplateFlowResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetTemplateFlowByKindQuery(kind);

                Result<TemplateFlowResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, ApiResponses.Problem);
            })
            .RequireAuthorization()
            .WithName("GetTemplateFlowByKind")
            .WithTags(EndpointTags.TemplateFlows)
            .Produces<TemplateFlowResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}
