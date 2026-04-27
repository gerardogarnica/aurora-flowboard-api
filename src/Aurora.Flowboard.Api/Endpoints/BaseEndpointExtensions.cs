using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Aurora.Flowboard.Api.Endpoints;

internal static class BaseEndpointExtensions
{
    private static readonly Assembly PresentationAssembly = typeof(BaseEndpointExtensions).Assembly;

    internal static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        ServiceDescriptor[] serviceDescriptor = [.. PresentationAssembly
            .GetTypes()
            .Where(x => x is { IsAbstract: false, IsInterface: false } &&
                        x.IsAssignableTo(typeof(IBaseEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IBaseEndpoint), type))];

        services.TryAddEnumerable(serviceDescriptor);

        return services;
    }

    internal static IApplicationBuilder MapEndpoints(
        this WebApplication app,
        RouteGroupBuilder? routeGroupBuilder = null)
    {
        IEnumerable<IBaseEndpoint> endpoints = app.Services.GetRequiredService<IEnumerable<IBaseEndpoint>>();

        IEndpointRouteBuilder builder = routeGroupBuilder is null
            ? app
            : routeGroupBuilder;

        foreach (IBaseEndpoint endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }

        return app;
    }
}
