using Aurora.Flowboard.Infrastructure.Authentication;
using Aurora.Flowboard.Infrastructure.Bootstrap;
using Aurora.Flowboard.Infrastructure.Cryptography;
using Aurora.Flowboard.Infrastructure.Database;
using Aurora.Flowboard.Infrastructure.DomainEvents;
using Aurora.Flowboard.Infrastructure.Interceptors;
using Aurora.Flowboard.Infrastructure.Outbox;
using Aurora.Flowboard.Infrastructure.Time;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Text;

namespace Aurora.Flowboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration) => services
            .AddAuthenticationServices(configuration)
            .AddDatabaseServices(configuration)
            .AddDateTimeServices()
            .AddDomainEventsServices()
            .AddPasswordHashingServices()
            .AddEncryptionServices(configuration)
            .AddBootstrapServices(configuration)
            .AddOutboxPatternImplementation()
            .AddQuartzServices();

    private static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtAuthOptions>(configuration.GetSection(JwtAuthOptions.SectionName));
        JwtAuthOptions jwtAuthOptions = configuration.GetSection(JwtAuthOptions.SectionName).Get<JwtAuthOptions>()!;

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<ITokenProvider, JwtTokenProvider>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtAuthOptions.Issuer,
                    ValidAudience = jwtAuthOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAuthOptions.Key)),
                };
            });

        services.AddAuthorization();

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

    private static IServiceCollection AddPasswordHashingServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        return services;
    }

    private static IServiceCollection AddEncryptionServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EncryptionOptions>(configuration.GetSection(EncryptionOptions.SectionName));

        services.AddTransient<EncryptionService>();

        return services;
    }

    private static IServiceCollection AddBootstrapServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BootstrapOptions>(configuration.GetSection(BootstrapOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddOutboxPatternImplementation(this IServiceCollection services)
    {
        services.AddOptions<OutboxOptions>().BindConfiguration("Outbox");
        services.ConfigureOptions<ConfigureProcessOutboxJob>();

        return services;
    }

    private static IServiceCollection AddQuartzServices(this IServiceCollection services)
    {
        services.AddQuartz();
        services.AddQuartzHostedService(cfg => cfg.WaitForJobsToComplete = true);

        return services;
    }
}
