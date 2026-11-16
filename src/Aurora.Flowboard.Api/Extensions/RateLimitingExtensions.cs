using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Aurora.Flowboard.Api.Extensions;

internal static class RateLimitingExtensions
{
    internal const string AuthPolicy = "Auth";

    private const string AuthSection = "RateLimiting:Auth";
    private const string PermitLimitKey = "PermitLimit";
    private const string WindowInSecondsKey = "WindowInSeconds";
    private const int DefaultPermitLimit = 10;
    private const int DefaultWindowInSeconds = 60;
    private const string UnknownClient = "unknown";

    // REMARK: Partitioned by client IP. Behind a reverse proxy, RemoteIpAddress is the proxy's
    // unless forwarded headers are enabled (ASPNETCORE_FORWARDEDHEADERS_ENABLED=true).
    internal static WebApplicationBuilder AddRateLimitingServices(this WebApplicationBuilder builder)
    {
        IConfigurationSection section = builder.Configuration.GetSection(AuthSection);
        int permitLimit = section.GetValue(PermitLimitKey, DefaultPermitLimit);
        int windowInSeconds = section.GetValue(WindowInSecondsKey, DefaultWindowInSeconds);

        // REMARK: The limiter only validates its options when the first request creates a partition,
        // so a non-positive value would turn every auth request into a 500 instead of failing here.
        if (permitLimit <= 0 || windowInSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"{AuthSection}:{PermitLimitKey} and {AuthSection}:{WindowInSecondsKey} must be greater than zero.");
        }

        TimeSpan window = TimeSpan.FromSeconds(windowInSeconds);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = window,
                        QueueLimit = 0
                    }));
        });

        return builder;
    }
}
