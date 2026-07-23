using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Aurora.Flowboard.Api.Extensions;

internal static class HealthCheckExtensions
{
    internal static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        return app;
    }
}
