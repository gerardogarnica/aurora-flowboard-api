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
            .AddUserTokenCleanupJob()
            .AddQuartzServices();

    private static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Without this, a missing or short key surfaces as a 500 on the first login, not at startup.
        // The message never includes the key.
        services.AddOptions<JwtAuthOptions>()
            .BindConfiguration(JwtAuthOptions.SectionName)
            .Validate(
                IsValidJwtAuthOptions,
                $"{JwtAuthOptions.SectionName} requires Issuer, Audience, a Key of at least " +
                $"{JwtAuthOptions.MinimumKeyLengthInBytes} bytes (UTF-8), LifeTimeInMinutes > 0 and RefreshTokenExpirationDays > 0.")
            .ValidateOnStart();

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

    private static bool IsValidJwtAuthOptions(JwtAuthOptions options) =>
        !string.IsNullOrWhiteSpace(options.Issuer) &&
        !string.IsNullOrWhiteSpace(options.Audience) &&
        !string.IsNullOrWhiteSpace(options.Key) &&
        Encoding.UTF8.GetByteCount(options.Key) >= JwtAuthOptions.MinimumKeyLengthInBytes &&
        options.LifeTimeInMinutes > 0 &&
        options.RefreshTokenExpirationDays > 0;

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

        // Readiness only: /health runs it, /alive (live tag) does not, so a database outage
        // never makes the container look dead.
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                DatabaseHealthCheck.Name,
                HealthStatus.Unhealthy,
                [DatabaseHealthCheck.ReadyTag],
                DatabaseHealthCheck.Timeout);

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
        // A zero interval or batch size would never drain the outbox, and a zero MaxAttempts would dead-letter every message without trying it.
        services.AddOptions<OutboxOptions>()
            .BindConfiguration(OutboxOptions.SectionName)
            .Validate(
                o => o.IntervalInSeconds > 0 && o.BatchSize > 0 && o.MaxAttempts > 0,
                $"{OutboxOptions.SectionName} requires IntervalInSeconds > 0, BatchSize > 0 and MaxAttempts > 0.")
            .ValidateOnStart();
        services.ConfigureOptions<ConfigureProcessOutboxJob>();

        return services;
    }

    private static IServiceCollection AddUserTokenCleanupJob(this IServiceCollection services)
    {
        // A zero batch size would loop forever, and a negative retention would delete live sessions.
        services.AddOptions<UserTokenCleanupOptions>()
            .BindConfiguration(UserTokenCleanupOptions.SectionName)
            .Validate(
                o => o.IntervalInHours > 0 && o.RetentionDays >= 0 && o.BatchSize > 0,
                $"{UserTokenCleanupOptions.SectionName} requires IntervalInHours > 0, RetentionDays >= 0 and BatchSize > 0.")
            .ValidateOnStart();
        services.ConfigureOptions<ConfigureUserTokenCleanupJob>();

        return services;
    }

    private static IServiceCollection AddQuartzServices(this IServiceCollection services)
    {
        services.AddQuartz();
        services.AddQuartzHostedService(cfg => cfg.WaitForJobsToComplete = true);

        return services;
    }
}
