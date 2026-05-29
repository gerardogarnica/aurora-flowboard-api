using Aurora.Flowboard.Api.Middlewares;
using Npgsql;
using OpenTelemetry.Resources;

namespace Aurora.Flowboard.Api;

internal static class DependencyInjection
{
    internal static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = "Flowboard API", Version = "v1" });
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
                .AddNpgsql());

        return builder;
    }
}
