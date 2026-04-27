namespace Aurora.Flowboard.Api.Endpoints;

public interface IBaseEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}