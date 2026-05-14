using Aurora.Flowboard.Application.Abstractions.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Aurora.Flowboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services) => services
        .AddMessagingHandlers()
        .AddBehaviors()
        .AddDomainHandlers()
        .AddDomainServices()
        .AddValidatorsFromAssembly();

    private static IServiceCollection AddMessagingHandlers(this IServiceCollection services)
    {
        services
            .Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services
            .Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services
            .Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }

    private static IServiceCollection AddBehaviors(this IServiceCollection services)
    {
        services.Decorate(typeof(IQueryHandler<,>), typeof(ValidationBehavior.QueryHandler<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationBehavior.CommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(ValidationBehavior.CommandBaseHandler<>));

        services.Decorate(typeof(IQueryHandler<,>), typeof(PerformanceBehavior.QueryHandler<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(PerformanceBehavior.CommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(PerformanceBehavior.CommandBaseHandler<>));

        services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingBehavior.QueryHandler<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingBehavior.CommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(LoggingBehavior.CommandBaseHandler<>));

        return services;
    }

    private static IServiceCollection AddDomainHandlers(this IServiceCollection services)
    {
        services
            .Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }

    private static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        return services;
    }

    private static IServiceCollection AddValidatorsFromAssembly(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}
