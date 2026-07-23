namespace Aurora.Flowboard.Api.Extensions;

internal static class CorsExtensions
{
    internal const string FrontendPolicy = "Frontend";

    internal static WebApplicationBuilder AddCorsServices(this WebApplicationBuilder builder)
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(FrontendPolicy, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return builder;
    }
}
