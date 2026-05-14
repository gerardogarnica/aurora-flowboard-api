using Aurora.Flowboard.Infrastructure.Authentication;
using Aurora.Flowboard.Infrastructure.Database;
using Aurora.Flowboard.Infrastructure.DomainEvents;
using Aurora.Flowboard.Infrastructure.Interceptors;
using Aurora.Flowboard.Infrastructure.Time;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Aurora.Flowboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration) => services
            .AddAuthenticationServices()
            .AddDatabaseServices(configuration)
            .AddDateTimeServices()
            .AddDomainEventsServices();

    private static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();

        return services;
    }

    private static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' not found.");

        services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    connectionString,
                    x => x.MigrationsHistoryTable(HistoryRepository.DefaultTableName, ApplicationDbContext.DefaultSchema))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        services.AddScoped<IApplicationDbContextFactory, ApplicationDbContextFactory>();
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

        // Entity Framework Core interceptors
        services.TryAddSingleton<InsertOutboxMessagesInterceptor>();

        return services;
    }

    private static IServiceCollection AddDateTimeServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();
        return services;
    }

    private static IServiceCollection AddDomainEventsServices(this IServiceCollection services)
    {
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();
        return services;
    }
}
