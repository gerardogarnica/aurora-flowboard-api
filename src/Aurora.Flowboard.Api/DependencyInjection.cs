using Aurora.Flowboard.Api.Middlewares;
using Microsoft.OpenApi;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Aurora.Flowboard.Api;

internal static class DependencyInjection
{
    private const string BearerSchemeName = "Bearer";
    private const string BearerScheme = "bearer";
    private const string JwtBearerFormat = "JWT";

    internal static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = "Flowboard API", Version = "v1" });

            options.AddSecurityDefinition(BearerSchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = BearerScheme,
                BearerFormat = JwtBearerFormat,
                Description = "Access token returned by POST auth/login."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerSchemeName, document)] = []
            });
        });

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return builder;
    }

    internal static WebApplicationBuilder AddErrorHandling(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails(cfg =>
        {
            cfg.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
            };
        });

        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        return builder;
    }

    internal static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(cfg => cfg
                .AddService(builder.Environment.ApplicationName))
            .WithTracing(cfg => cfg
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddNpgsql())
            .WithMetrics(cfg => cfg
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation());

        builder.Logging.AddOpenTelemetry(cfg =>
        {
            cfg.IncludeScopes = true;
            cfg.IncludeFormattedMessage = true;
        });

        return builder;
    }

    internal static WebApplicationBuilder AddHealthCheckServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();

        return builder;
    }
}
